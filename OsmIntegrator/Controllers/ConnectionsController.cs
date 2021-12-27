using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

namespace OsmIntegrator.Controllers
{
  [Produces(MediaTypeNames.Application.Json)]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ApiController]
  [Route("api/[controller]")]
  [EnableCors("AllowOrigin")]
  public class ConnectionsController : ControllerBase
  {
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConnectionsController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ITileValidator _tileValidator;
    private readonly IStringLocalizer<ConnectionsController> _localizer;

    public ConnectionsController(ApplicationDbContext dbContext,
        ILogger<ConnectionsController> logger, IMapper mapper,
        UserManager<ApplicationUser> userManager,
        ITileValidator tileValidator,
        IStringLocalizer<ConnectionsController> localizer)
    {
      _dbContext = dbContext;
      _logger = logger;
      _mapper = mapper;
      _userManager = userManager;
      _tileValidator = tileValidator;
      _localizer = localizer;
    }

    /// <summary>
    /// Add/remove a connection for specific user.
    /// </summary>
    /// <param name="id">Tile id</param>
    /// <param name="connectionAction">Add or remove connection action</param>
    /// <returns>Generic result or error.</returns>
    [HttpPut()]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
    public async Task<IActionResult> Add([FromBody] NewConnectionAction connectionAction)
    {

      // Check if connection already exists and has not been deleted.
      List<DbConnection> existingConnections = await _dbContext.Connections
          .Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                  x.GtfsStopId == connectionAction.GtfsStopId)
          .OrderByDescending(link => link.CreatedAt)
          .ToListAsync();

      DbConnection existingConnection = existingConnections.FirstOrDefault();
      bool imported = false;
      if (existingConnection != null)
      {
        if (existingConnection.OperationType == ConnectionOperationType.Added)
        {
          throw new BadHttpRequestException(_localizer["The connection already exists"]);
        }
        // Check wether last connection was imported
        imported = existingConnection.Imported;
      }

      // Check if GTFS stop has already been connected to another stop.
      DbStop gtfsStop = await _dbContext.Stops
          .Include(x => x.GtfsConnections)
          .FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);

      if (gtfsStop == null)
      {
        throw new BadHttpRequestException(_localizer["Please ensure correct stops were chosen"]);
      }

      DbConnection gtfsConnection = gtfsStop.GtfsConnections
          .OrderByDescending(link => link.CreatedAt)
          .FirstOrDefault();

      if (gtfsConnection != null && gtfsConnection.OperationType == ConnectionOperationType.Added)
      {
        throw new BadHttpRequestException(_localizer["The GTFS stop has already been connected with different stop"]);
      }

      // Check if OSM stop has already been connected to another stop.
      DbStop osmStop = await _dbContext.Stops
          .Include(x => x.OsmConnections)
          .Include(x => x.Tile)
          .FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);

      if (osmStop == null)
      {
        throw new BadHttpRequestException(_localizer["Please ensure correct stops were chosen"]);
      }

      if(osmStop.StopType == gtfsStop.StopType)
      {
        throw new BadHttpRequestException(_localizer["Stops cannot have the same type"]);
      }

      DbConnection osmConnection = osmStop.OsmConnections
          .OrderByDescending(link => link.CreatedAt)
          .FirstOrDefault();

      if (osmConnection != null && osmConnection.OperationType == ConnectionOperationType.Added)
      {
        throw new BadHttpRequestException(_localizer["The OSM stop has already been connected with different stop"]);
      }

      // Check if OSM stop is inside a tile (this is mandatory)
      if (osmStop.Tile.Id != connectionAction.TileId)
      {
        throw new BadHttpRequestException(_localizer["OSM stop needs to be placed inside the tile"]);
      }

      ApplicationUser currentUser = await _userManager.GetUserAsync(User);

      DbConnection newConnection = new DbConnection()
      {
        OsmStop = osmStop,
        OsmStopId = osmStop.Id,
        GtfsStopId = gtfsStop.Id,
        GtfsStop = gtfsStop,
        User = currentUser,
        Imported = imported,
        OperationType = ConnectionOperationType.Added
      };

      _dbContext.Connections.Add(newConnection);
      _dbContext.SaveChanges();

      return Ok(_localizer["Connection successfully added!"]);
    }

    [HttpDelete()]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
    public async Task<IActionResult> Remove([FromBody] ConnectionAction connectionAction)
    {
      List<DbConnection> existingConnections = await _dbContext.Connections.Where(x => x.OsmStopId == connectionAction.OsmStopId &&
          x.GtfsStopId == connectionAction.GtfsStopId).ToListAsync();
      DbConnection existingConnection = existingConnections.LastOrDefault();
      bool imported = false;
      if (existingConnection != null)
      {
        if (existingConnection.OperationType == ConnectionOperationType.Removed)
        {
          throw new BadHttpRequestException(_localizer["The connection have already been removed"]);
        }
        // Check wether last connection was imported
        imported = existingConnection.Imported;
      }
      else if (existingConnection == null)
      {
        throw new BadHttpRequestException(_localizer["Connot remove connection which doesn't exist"]);
      }

      ApplicationUser currentUser = await _userManager.GetUserAsync(User);

      DbConnection newConnection = new DbConnection()
      {
        OsmStopId = (Guid)connectionAction.OsmStopId,
        GtfsStopId = (Guid)connectionAction.GtfsStopId,
        User = currentUser,
        Imported = imported,
        OperationType = ConnectionOperationType.Removed
      };

      _dbContext.Connections.Add(newConnection);
      _dbContext.SaveChanges();

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

      List<DbConnection> connections = await _dbContext.Connections.FromSqlRaw(
          query).Include(x => x.OsmStop).ToListAsync();

      connections = connections.Where(
          x => x.OsmStop.TileId == Guid.Parse(id) && x.OperationType != ConnectionOperationType.Removed)
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

    [HttpPut("Approve/{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR)]
    public async Task<ActionResult<string>> Approve(string id)
    {

      DbConnection link = await _dbContext.Connections.Where(c => c.Id == Guid.Parse(id)).FirstOrDefaultAsync();
      if (link == null)
      {
        throw new BadHttpRequestException(_localizer["Given connection does not exist"]);
      }
      ApplicationUser currentUser = await _userManager.GetUserAsync(User);
      link.ApprovedBy = currentUser;
      _dbContext.SaveChanges();

      return Ok(_localizer["Connection approved"]);
    }
    [HttpPut("Unapprove/{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR)]
    public async Task<ActionResult<string>> Unapprove(string id)
    {

      DbConnection link = await _dbContext.Connections.Where(c => c.Id == Guid.Parse(id)).FirstOrDefaultAsync();
      if (link == null)
      {
        throw new BadHttpRequestException(_localizer["Given connection does not exist"]);
      }
      link.ApprovedById = null;
      _dbContext.SaveChanges();

      return Ok(_localizer["Connection unapproved"]);
    }
  }


}