﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Roles;
using OsmIntegrator.Tools;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Extensions;
using OsmIntegrator.Validators;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Enums;
using OsmIntegrator.Database.QueryObjects;

namespace OsmIntegrator.Controllers
{
  [Produces(MediaTypeNames.Application.Json)]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ApiController]
  [Route("api/[controller]/[action]")]
  [EnableCors("AllowOrigin")]
  public class TileController : ControllerBase
  {
    private readonly ILogger<TileController> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<TileController> _localizer;
    private readonly IOverpass _overpass;
    private readonly IOsmUpdater _osmUpdater;
    private readonly ITileExportValidator _tileExportValidator;

    public TileController(
        ILogger<TileController> logger,
        ApplicationDbContext dbContext,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<TileController> localizer,
        IOsmUpdater refresherHelper,
        IOverpass overpass,
        ITileExportValidator tileExportValidator)
    {
      _logger = logger;
      _dbContext = dbContext;
      _mapper = mapper;
      _userManager = userManager;
      _localizer = localizer;
      _osmUpdater = refresherHelper;
      _overpass = overpass;
      _tileExportValidator = tileExportValidator;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<List<Tile>>> GetTiles()
    {
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      List<DbTile> tiles = new List<DbTile>();

      if (roles.Contains(UserRoles.EDITOR))
      {

        tiles = await _dbContext.Tiles
          .Include(x => x.Stops).ThenInclude(x => x.GtfsConnections)
          .Where(x => x.Stops.Any(s => s.StopType == StopType.Gtfs))
          .OnlyAccessibleBy(user.Id)
          .ToListAsync();
      }

      return Ok(_mapper.Map<List<Tile>>(tiles));
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.SUPERVISOR)]
    public async Task<ActionResult<List<Tile>>> GetUncommitedTiles()
    {
      return Ok(await GetUncommitedTilesAsync());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<List<Stop>>> GetStops(string id)
    {
      // Validate tile id
      if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid tileId))
      {
        throw new BadHttpRequestException(_localizer["Invalid tile"]);
      }

      // Check if tile exists
      DbTile tile = await _dbContext.Tiles
          .Include(t => t.Stops)
            .ThenInclude(s => s.GtfsConnections)
          .SingleOrDefaultAsync(x => x.Id == tileId);
      if (tile == null)
      {
        throw new BadHttpRequestException(_localizer["Unable to find tile"]);
      }

      // Get current user roles
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      // Check if the user is assigned to a tile?
      if (!tile.IsAccessibleBy(user.Id))
      {
        throw new BadHttpRequestException(_localizer["You are unable to edit this tile"]);
      }

      // Get all stops in selected tile + stops around that tile
      var stops = await _dbContext.Stops
        .Where(x =>
          x.Lon > tile.OverlapMinLon && x.Lon <= tile.OverlapMaxLon &&
          x.Lat > tile.OverlapMinLat && x.Lat <= tile.OverlapMaxLat)
        .Where(x => !x.IsDeleted)
        .ToListAsync();

      foreach (DbStop stop in stops)
      {
        if (stop.Lon > tile.MinLon && stop.Lon <= tile.MaxLon &&
            stop.Lat > tile.MinLat && stop.Lat <= tile.MaxLat)
        {
          stop.OutsideSelectedTile = false;
          continue;
        }
        stop.OutsideSelectedTile = true;
      }

      // Remove GTFS stops outside the tile
      stops.RemoveAll(x => x.OutsideSelectedTile && x.StopType == StopType.Gtfs);

      tile.Stops = stops;
      tile.Stops.ForEach(x => x.Tile = null);
      List<Stop> result = _mapper.Map<List<Stop>>(tile.Stops);
      return Ok(result);
    }

    /// <summary>
    /// Assign a user to all not exported connections in the tile.
    /// </summary>
    /// <param name="id">Tile id</param>
    /// <param name="updateTileInput">New user id</param>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<string>> UpdateUsers(Guid id, [FromBody] UpdateTileInput updateTileInput)
    {
      if (updateTileInput.EditorId == null)
      {
        throw new BadHttpRequestException(_localizer["EditorId cannot be null"]);
      }
      
      List<DbConnection> connections = (await _dbContext.Connections
        .Where(c => c.GtfsStop.TileId == id)
        .ToListAsync())
        .OnlyActive()
        .Where(c => c.UserId != updateTileInput.EditorId)
        .ToList();

      connections.ForEach(x => x.UserId = (Guid)updateTileInput.EditorId);

      await _dbContext.SaveChangesAsync();

      return Ok(_localizer["User has been added to the tile"]);
    }

    [HttpGet("{tileId}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<bool>> ContainsChanges(Guid tileId)
    {
      DbTile tile = await GetTileAsync(tileId);

      Osm osm = await _overpass.GetArea(tile.MinLat, tile.MinLon, tile.MaxLat, tile.MaxLon);

      return _osmUpdater.ContainsChanges(tile, osm);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<Report>> UpdateStops(Guid id)
    {
      if (!await _tileExportValidator.ValidateDelayAsync(id))
      {
        throw new BadHttpRequestException(_localizer["Delay required"]);
      }

      DbTile tile = await GetTileAsync(id);

      Osm osm = await _overpass.GetArea(tile.MinLat, tile.MinLon, tile.MaxLat, tile.MaxLon);

      TileImportReport tileReport = await _osmUpdater.Update(tile, _dbContext, osm);

      return Ok(new Report { Value = tileReport.ToString() });
    }

    private async Task<DbTile> GetTileAsync(Guid tileId)
    {
      DbTile currentTile = await _dbContext.Tiles
        .Include(tile => tile.Stops)
        .SingleOrDefaultAsync(x => x.Id == tileId);

      if (currentTile == null)
      {
        throw new BadHttpRequestException(_localizer["Given tile does not exist"]);
      }

      return currentTile;
    }

    private static Dictionary<Guid, List<ConnectionQuery>> ActiveConnections(List<ConnectionQuery> connections) =>
      connections
        .GroupBy(c => new { c.GtfsStopId, c.OsmStopId })
        .Select(cg => cg.OrderByDescending(c => c.CreatedAt).FirstOrDefault())
        .Where(c => c?.OperationType == ConnectionOperationType.Added)
        .GroupBy(c => c.TileId)
        .ToDictionary(c => c.Key, c => c.ToList());

    private async Task<IReadOnlyCollection<UncommitedTile>> GetUncommitedTilesAsync()
    {
      List<ConnectionQuery> connections =
        await _dbContext.GetAllConnectionsWithUserName();

      Dictionary<Guid, List<ConnectionQuery>> activeConnectionsTileGroup =
        ActiveConnections(connections);

      List<TileQuery> dbTiles = await _dbContext.GetTileQuery(activeConnectionsTileGroup);

      List<UncommitedTile> uncommitedTiles =
        dbTiles.Join(activeConnectionsTileGroup, t => t.Id, g => g.Key, (t, g) =>
          new UncommitedTile
          {
            Id = t.Id,
            Y = t.Y,
            X = t.X,
            MinLon = t.MinLon,
            MinLat = t.MinLat,
            MaxLat = t.MaxLat,
            MaxLon = t.MaxLon,
            AssignedUserName = g.Value.FirstOrDefault(c => c.UserName != null)?.UserName,
            GtfsStopsCount = t.GtfsStopsCount,
            UnconnectedGtfsStops = t.GtfsStopsCount - g.Value.Count(c => c.TileId == t.Id)
          })
          .ToList();

      return uncommitedTiles;
    }
  }
}
