using System;
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
using Microsoft.Extensions.Configuration;
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
using OsmIntegrator.Errors;
using OsmIntegrator.Enums;

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
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly RoleManager<ApplicationRole> _roleManger;
    private readonly IStringLocalizer<TileController> _localizer;
    private readonly IOverpass _overpass;
    private readonly IOsmUpdater _osmUpdater;
    private readonly ITileExportValidator _tileExportValidator;

    public TileController(
        ILogger<TileController> logger,
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IStringLocalizer<TileController> localizer,
        IEmailService emailService,
        IOsmUpdater refresherHelper,
        IOverpass overpass, 
        ITileExportValidator tileExportValidator)
    {
      _logger = logger;
      _dbContext = dbContext;
      _configuration = configuration;
      _mapper = mapper;
      _userManager = userManager;
      _roleManger = roleManager;
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
        List<DbTile> editorTiles = await _dbContext.Tiles
          .Include(x => x.Stops).ThenInclude(x => x.GtfsConnections)
          .Where(x => x.Stops.Any(s => s.StopType == StopType.Gtfs))
          .OnlyAccessibleBy(user.Id)
          .ToListAsync();
        tiles.AddRange(editorTiles);
      }

      return Ok(_mapper.Map<List<Tile>>(tiles));
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.SUPERVISOR)]
    public async Task<ActionResult<List<Tile>>> GetUncommitedTiles()
    {
      List<DbTile> tiles = (await _dbContext.Connections
        .Where(c => c.UserId != null)
        .Include(c => c.GtfsStop.Tile)
          .ThenInclude(t => t.Stops.Where(s => s.StopType == StopType.Gtfs))
          .ThenInclude(s => s.GtfsConnections)
          .ThenInclude(c => c.User)
        .ToListAsync())
        .OnlyActive()
        .Select(c => c.GtfsStop.Tile)
        .Distinct()
        .ToList();

      return Ok(_mapper.Map<List<UncommitedTile>>(tiles));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<List<Stop>>> GetStops(string id)
    {
      // Validate tile id
      Guid tileId;
      if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
      {
        throw new BadHttpRequestException(_localizer["Invalid tile"]);
      }

      // Check if tile exists
      var tile = await _dbContext.Tiles
          .Include(t => t.Stops).ThenInclude(s => s.GtfsConnections)
          .SingleOrDefaultAsync(x => x.Id == tileId);
      if (tile == null)
      {
        throw new BadHttpRequestException(_localizer["Unable to find tile"]);
      }

      // Get current user roles
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      // Check if user is assigned to a tile?
      // When user is SUPERVISOR or ADMIN this validation is not required.
      if (
        !roles.Contains(UserRoles.SUPERVISOR) &&
        !roles.Contains(UserRoles.COORDINATOR) &&
        (roles.Contains(UserRoles.EDITOR) && !tile.IsAccessibleBy(user.Id)))
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

      // Remove OSM stops outside the tile
      stops.RemoveAll(x => x.OutsideSelectedTile && x.StopType == 0);

      tile.Stops = stops;
      tile.Stops.ForEach(x => x.Tile = null);
      List<Stop> result = _mapper.Map<List<Stop>>(tile.Stops);
      return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<string>> UpdateUsers(Guid id, [FromBody] UpdateTileInput updateTileInput)
    {
      List<DbConnection> connections = (await _dbContext.Connections
        .Where(c => c.GtfsStop.TileId == id)
        .Where(c => c.UserId != null)
        .ToListAsync())
        .OnlyActive()
        .Where(c => c.UserId != updateTileInput.EditorId)
        .ToList();

      foreach (var connection in connections)
      {
        connection.UserId = updateTileInput.EditorId;
      }

      _dbContext.SaveChanges();

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
  }
}
