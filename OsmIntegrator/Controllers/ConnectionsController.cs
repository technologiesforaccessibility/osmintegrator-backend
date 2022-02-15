using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Roles;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Validators;
using OsmIntegrator.Enums;
using Microsoft.Extensions.Localization;
using OsmIntegrator.ApiModels.Connections;

namespace OsmIntegrator.Controllers;

[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowOrigin")]
public class ConnectionsController : ControllerBase
{
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IMapper _mapper;
  private readonly ITileValidator _tileValidator;
  private readonly IStringLocalizer<ConnectionsController> _localizer;

  public ConnectionsController(ApplicationDbContext dbContext,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ITileValidator tileValidator,
    IStringLocalizer<ConnectionsController> localizer)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _userManager = userManager;
    _tileValidator = tileValidator;
    _localizer = localizer;
  }

  /// <summary>
  /// Add/remove a connection for specific user.
  /// </summary>
  /// <param name="connectionAction">Add or remove connection action</param>
  /// <returns>Generic result or error.</returns>
  [HttpPut()]
  [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
  public async Task<IActionResult> Add([FromBody] NewConnectionAction connectionAction)
  {
    // Check if connection already exists
    DbConnection existingConnection = await _dbContext.Connections
      .Where(x => x.OsmStopId == connectionAction.OsmStopId)
      .Where(x => x.GtfsStopId == connectionAction.GtfsStopId)
      .OrderByDescending(link => link.CreatedAt)
      .AsNoTracking()
      .FirstOrDefaultAsync();

    // Check if connection has not been added.
    if (existingConnection?.OperationType == ConnectionOperationType.Added)
    {
      throw new BadHttpRequestException(_localizer["The connection already exists"]);
    }

    // Check if GTFS stop has already been connected to another stop.
    DbStop gtfsStop = await _dbContext.Stops
      .Include(s => s.GtfsConnections)
      .Include(x => x.Tile)
      .FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);

    if (gtfsStop == null)
    {
      throw new BadHttpRequestException(_localizer["Please ensure correct stops were chosen"]);
    }

    DbConnection gtfsConnection = gtfsStop.GtfsConnections
      .OrderByDescending(x => x.CreatedAt)
      .FirstOrDefault();

    if (gtfsConnection?.OperationType == ConnectionOperationType.Added)
    {
      throw new BadHttpRequestException(_localizer["The GTFS stop has already been connected with different stop"]);
    }

    // Check if OSM stop has already been connected to another stop.
    DbStop osmStop = await _dbContext.Stops
      .Include(x => x.OsmConnections)
      .FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);

    if (osmStop == null)
    {
      throw new BadHttpRequestException(_localizer["Please ensure correct stops were chosen"]);
    }

    if (osmStop.StopType == gtfsStop.StopType)
    {
      throw new BadHttpRequestException(_localizer["Stops cannot have the same type"]);
    }

    DbConnection osmConnection = osmStop.OsmConnections
      .OrderByDescending(link => link.CreatedAt)
      .FirstOrDefault();

    if (osmConnection?.OperationType == ConnectionOperationType.Added)
    {
      throw new BadHttpRequestException(_localizer["The OSM stop has already been connected with different stop"]);
    }

    // Check if OSM stop is inside a tile (this is mandatory)
    if (gtfsStop.Tile.Id != connectionAction.TileId)
    {
      throw new BadHttpRequestException(_localizer["GTFS stop needs to be placed inside the tile"]);
    }

    DbTile tile = gtfsStop.Tile;
    if (osmStop.Lon <= tile.OverlapMinLon ||
        osmStop.Lon > tile.OverlapMaxLon ||
        osmStop.Lat <= tile.OverlapMinLat ||
        osmStop.Lat > tile.OverlapMaxLat)
    {
      throw new BadHttpRequestException(_localizer["OSM stop is outside of the margin"]);
    }

    ApplicationUser currentUser = await _userManager.GetUserAsync(User);

    if (!gtfsStop.Tile.IsAccessibleBy(currentUser.Id))
    {
      throw new BadHttpRequestException(_localizer["You are unable to edit this tile"]);
    }

    DbConnection newConnection = new()
    {
      OsmStop = osmStop,
      OsmStopId = osmStop.Id,
      GtfsStopId = gtfsStop.Id,
      GtfsStop = gtfsStop,
      User = currentUser,
      OperationType = ConnectionOperationType.Added
    };

    await _dbContext.Connections.AddAsync(newConnection);
    await _dbContext.SaveChangesAsync();

    return Ok(_localizer["Connection successfully added!"]);
  }

  [HttpPost("Remove")]
  [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
  public async Task<IActionResult> Remove([FromBody] ConnectionAction connectionAction)
  {
    if (connectionAction.OsmStopId == null || connectionAction.GtfsStopId == null)
    {
      throw new BadHttpRequestException(_localizer["OsmStopId or GtfsStopId cannot be null"]);
    }

    List<DbConnection> existingConnections =
      await _dbContext.Connections
        .Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                    x.GtfsStopId == connectionAction.GtfsStopId)
        .ToListAsync();
    DbConnection existingConnection = existingConnections.LastOrDefault();

    if (existingConnection != null)
    {
      if (existingConnection.OperationType == ConnectionOperationType.Removed)
      {
        throw new BadHttpRequestException(_localizer["The connection have already been removed"]);
      }
    }
    else if (existingConnection == null)
    {
      throw new BadHttpRequestException(_localizer["Cannot remove connection which doesn't exist"]);
    }

    ApplicationUser currentUser = await _userManager.GetUserAsync(User);

    DbConnection newConnection = new()
    {
      OsmStopId = (Guid) connectionAction.OsmStopId,
      GtfsStopId = (Guid) connectionAction.GtfsStopId,
      User = currentUser,
      OperationType = ConnectionOperationType.Removed
    };

    await _dbContext.Connections.AddAsync(newConnection);
    await _dbContext.SaveChangesAsync();

    return Ok(_localizer["Connection successfully removed!"]);
  }


  /// <summary>
  /// Get connections for tile id.
  /// </summary>
  /// <param name="id">Tile id.</param>
  /// <returns>Collection of connections in selected tile.</returns>
  [HttpGet("{id}")]
  [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
  public async Task<ActionResult<List<Connection>>> Get(string id)
  {
    Error error = await _tileValidator.Validate(_dbContext, id);
    if (error != null) throw new BadHttpRequestException(_localizer["Problem with given tile"]);

    string query =
      "SELECT DISTINCT ON (\"GtfsStopId\", \"OsmStopId\") * " +
      "FROM \"Connections\" c " +
      "ORDER BY \"GtfsStopId\", \"OsmStopId\", \"CreatedAt\" DESC";

    List<DbConnection> connections = await _dbContext.Connections.FromSqlRaw(query)
      .Include(x => x.GtfsStop)
      .ToListAsync();

    connections = connections.Where(
        x => x.GtfsStop.TileId == Guid.Parse(id) && x.OperationType != ConnectionOperationType.Removed)
      .ToList();

    List<Connection> result = _mapper.Map<List<Connection>>(connections);
    return Ok(result);
  }

  /// <summary>
  /// Get all connections.
  /// </summary>
  /// <returns>All existing and not existing connections.</returns>
  [HttpGet()]
  [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
  public async Task<ActionResult<List<Connection>>> GetAll()
  {
    List<DbConnection> connections = await _dbContext.Connections.FromSqlRaw(
      "SELECT DISTINCT ON (\"GtfsStopId\", \"OsmStopId\") * " +
      "FROM \"Connections\" c " +
      "ORDER BY \"GtfsStopId\", \"OsmStopId\", \"CreatedAt\" DESC"
    ).ToListAsync();
    List<Connection> result = _mapper.Map<List<Connection>>(connections);
    return Ok(result);
  }
}