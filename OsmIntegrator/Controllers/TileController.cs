using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using MimeKit;
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
using OsmIntegrator.Database.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    readonly IOsmUpdater _osmUpdater;

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
        IOverpass overpass
    )
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
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult<List<Tile>>> GetAllTiles()
    {
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      List<DbTile> tiles = new List<DbTile>();

      if (roles.Contains(UserRoles.SUPERVISOR))
      {
        List<DbTile> supervisorTiles = await _dbContext.Tiles
          .Include(x => x.TileUsers)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x => x.SupervisorApprovedId == null)
          .ToListAsync();
        tiles.AddRange(supervisorTiles);
      }

      if (roles.Contains(UserRoles.COORDINATOR))
      {
        List<DbTile> coordinatorTiles = await _dbContext.Tiles
          .Include(x => x.TileUsers)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x => x.EditorApprovedId != null && x.SupervisorApprovedId != null).ToListAsync();
        tiles.AddRange(coordinatorTiles);
      }

      return Ok(_mapper.Map<List<Tile>>(tiles));
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<List<Tile>>> GetTiles()
    {
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      List<DbTile> tiles = new List<DbTile>();

      if (roles.Contains(UserRoles.SUPERVISOR))
      {
        List<DbTile> supervisorTiles = await _dbContext.Tiles
          .Include(x => x.Stops).ThenInclude(x => x.GtfsConnections).ThenInclude(c => c.User)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x =>
            x.EditorApprovedId != null
            && x.SupervisorApprovedId == null
            && x.TileUsers.Any(y =>
              y.User.Id == user.Id
              && y.Role.Name == UserRoles.SUPERVISOR
            )
          )
          .ToListAsync();
        tiles.AddRange(supervisorTiles);
      }

      if (roles.Contains(UserRoles.COORDINATOR))
      {
        List<DbTile> coordinatorTiles = await _dbContext.Tiles
          .Where(
            x => x.GtfsStopsCount > 0 && x.EditorApprovedId != null && x.SupervisorApprovedId != null)
          .ToListAsync();
        tiles.AddRange(coordinatorTiles);
      }

      if (roles.Contains(UserRoles.EDITOR))
      {
        List<DbTile> editorTiles = await _dbContext.Tiles
          .Include(x => x.Stops).ThenInclude(x => x.GtfsConnections)
          .Where(x => x.GtfsStopsCount > 0)
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
        .Include(c => c.GtfsStop.GtfsConnections).ThenInclude(c => c.User)
        .ToListAsync())
        .OnlyActive()
        .Select(c => c.GtfsStop.Tile)
        .Distinct()
        .ToList();

      return Ok(_mapper.Map<List<Tile>>(tiles));
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
          .Include(x => x.TileUsers).ThenInclude(y => y.User)
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

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<TileWithUsers>> GetUsers(string id)
    {
      Guid tileId;
      if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
      {
        throw new BadHttpRequestException(_localizer["Invalid tile"]);
      }

      List<ApplicationUser> allUsers = await _userManager.Users.OrderBy(x => x.UserName).ToListAsync();

      // Remove other users than editors
      List<ApplicationUser> editors = new List<ApplicationUser>();
      foreach (ApplicationUser user in allUsers)
      {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        // add roles to user
        user.Roles = roles;
        if (roles.Any(x => x == UserRoles.EDITOR || x == UserRoles.SUPERVISOR))
          editors.Add(user);
      }

      // Get current tile by id
      DbTile currentTile =
          await _dbContext.Tiles
            .Include(tile => tile.TileUsers)
              .ThenInclude(y => y.User)
            .Include(tile => tile.TileUsers)
              .ThenInclude(y => y.Role)
            .SingleOrDefaultAsync(x => x.Id == tileId);

      TileWithUsers result = new TileWithUsers
      {
        Id = tileId,
        Users = new List<TileUser>()
      };

      foreach (ApplicationUser user in editors)
      {
        TileUser tileUser = new TileUser
        {
          Id = user.Id,
          UserName = user.UserName,
          IsSupervisor = user.Roles.Contains(UserRoles.SUPERVISOR),
          IsEditor = user.Roles.Contains(UserRoles.EDITOR)
        };
        result.Users.Add(tileUser);
        tileUser.IsAssigned = currentTile.TileUsers.Any(x => x.User.Id == user.Id && x.Role.Name == UserRoles.EDITOR);
        tileUser.IsAssignedAsSupervisor = currentTile.TileUsers.Any(x => x.User.Id == user.Id && x.Role.Name == UserRoles.SUPERVISOR);
      }

      return Ok(result);

    }

    /// <summary>
    /// DEPRECATED
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<string>> RemoveUser(string id)
    {
      DbTile currentTile = await GetTileAsync(id);

      if (currentTile == null)
      {
        throw new BadHttpRequestException(_localizer["Unable to find tile"]);
      }

      if (currentTile.EditorApproved != null)
      {
        throw new BadHttpRequestException(_localizer["Unable to remove user from already approved tile"]);
      }

      currentTile.TileUsers.Clear();
      _dbContext.SaveChanges();

      return Ok(_localizer["User successfully removed from the tile"]);
    }

    /// <summary>
    /// DEPRECATED
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<string>> UpdateUser(string id, [FromBody] User userBody)
    {
      ApplicationUser user = await _userManager.FindByIdAsync(userBody.Id.ToString());

      IList<string> roles = await _userManager.GetRolesAsync(user);
      if (!roles.Contains(UserRoles.EDITOR))
      {
        throw new BadHttpRequestException(_localizer["This user is not an editor"]);
      }

      DbTile currentTile = await GetTileAsync(id);

      if (currentTile.EditorApproved != null)
      {
        throw new BadHttpRequestException(_localizer["Unable to assign. Tile has already been approved by editor"]);
      }

      ApplicationRole editorRole = _roleManger.Roles.Where(x => x.Name == UserRoles.EDITOR).First();

      _dbContext.TileUsers.RemoveRange(_dbContext.TileUsers.Where(x => x.Tile == currentTile && x.Role == editorRole));

      _dbContext.TileUsers.Add(new DbTileUser()
      {
        Id = new Guid(),
        User = user,
        Tile = currentTile,
        Role = editorRole
      });

      _dbContext.SaveChanges();

      return Ok(_localizer["User has been added to the tile"]);
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

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.EDITOR)]
    public async Task<ActionResult<string>> Approve(string id)
    {
      DbTile currentTile = await GetTileAsync(id);

      ApplicationUser user = await _userManager.GetUserAsync(User);

      List<string> userRoles = (List<string>)await _userManager.GetRolesAsync(user);

      if (currentTile.SupervisorApproved != null)
      {
        throw new BadHttpRequestException(_localizer["Tile has already been approved by supervisor"]);
      }

      if (currentTile.EditorApproved != null && userRoles.Contains(UserRoles.SUPERVISOR))
      {
        if (currentTile.TileUsers.Any(x => x.Id == user.Id && x.Role.Name == UserRoles.EDITOR))
        {
          throw new BadHttpRequestException(_localizer["Supervisor cannot approve tile edited by himself"]);
        }

        currentTile.SupervisorApproved = user;
        currentTile.SupervisorApprovalTime = DateTime.Now;
      }
      else if (currentTile.EditorApproved == null && userRoles.Contains(UserRoles.EDITOR))
      {
        currentTile.EditorApproved = user;
        currentTile.EditorApprovalTime = DateTime.Now;
      }
      else
      {
        _logger.LogError($"Problem with assigning user to tile. Tile id: {currentTile.Id}, editor approved: {currentTile.EditorApproved != null}, supervisor approved: {currentTile.SupervisorApproved != null}.");
        throw new BadHttpRequestException(_localizer["Something went wrong when assigning user to a tile"]);
      }

      _dbContext.SaveChanges();

      return Ok(_localizer["Tile approved"]);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<Report>> UpdateStops(string id)
    {
      DbTile tile = await GetTileAsync(id);

      Osm osm = await _overpass.GetArea(tile.MinLat, tile.MinLon, tile.MaxLat, tile.MaxLon);

      ReportTile tileReport = await _osmUpdater.Update(tile, _dbContext, osm);

      return Ok(new Report { Value = tileReport.ToString() });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<bool>> ContainsChanges(string id)
    {
      DbTile tile = await GetTileAsync(id);

      Osm osm = await _overpass.GetArea(tile.MinLat, tile.MinLon, tile.MaxLat, tile.MaxLon);

      bool containsChanges = _osmUpdater.ContainsChanges(tile, osm);

      return Ok(containsChanges);
    }

    private async Task<DbTile> GetTileAsync(string tileId)
    {
      DbTile currentTile = await _dbContext.Tiles
        .Include(tile => tile.TileUsers).ThenInclude(y => y.User)
        .Include(tile => tile.TileUsers).ThenInclude(y => y.Role)
        .Include(tile => tile.Stops)
        .SingleOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
      if (currentTile == null)
      {
        throw new BadHttpRequestException(_localizer["Given tile does not exist"]);
      }
      return currentTile;
    }
  }
}
