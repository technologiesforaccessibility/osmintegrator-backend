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
    private readonly IEmailService _emailService;

    public TileController(
        ILogger<TileController> logger,
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IStringLocalizer<TileController> localizer,
        IEmailService emailService
    )
    {
      _logger = logger;
      _dbContext = dbContext;
      _configuration = configuration;
      _mapper = mapper;
      _userManager = userManager;
      _roleManger = roleManager;
      _localizer = localizer;
      _emailService = emailService;
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
        List<DbTile> supervisorTiles = await _dbContext.Tiles.Include(x => x.Users)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x => x.SupervisorApprovedId == null)
          .ToListAsync();
        tiles.AddRange(supervisorTiles);
      }

      if (roles.Contains(UserRoles.COORDINATOR))
      {
        List<DbTile> coordinatorTiles = await _dbContext.Tiles.Include(x => x.Users)
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
        List<DbTile> supervisorTiles = await _dbContext.Tiles.Include(x => x.Users)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x => x.EditorApprovedId != null && x.SupervisorApprovedId == null)
          .Where(x => !x.Users.Any(x => x.Id == user.Id))
          .ToListAsync();
        tiles.AddRange(supervisorTiles);
      }

      if (roles.Contains(UserRoles.COORDINATOR))
      {
        List<DbTile> coordinatorTiles = await _dbContext.Tiles.Include(x => x.Users).Where(
          x => x.GtfsStopsCount > 0 && x.EditorApprovedId != null && x.SupervisorApprovedId != null).ToListAsync();
        tiles.AddRange(coordinatorTiles);
      }

      if (roles.Contains(UserRoles.EDITOR))
      {
        List<DbTile> editorTiles = await _dbContext.Tiles.Include(x => x.Users)
          .Where(x => x.GtfsStopsCount > 0)
          .Where(x => x.Users.Any(x => x.Id == user.Id))
          .Where(x => x.EditorApprovedId == null && x.SupervisorApprovedId == null)
          .ToListAsync();
        tiles.AddRange(editorTiles);
      }

      return Ok(_mapper.Map<List<Tile>>(tiles));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<Stop>> GetStops(string id)
    {
      // Validate tile id
      Guid tileId;
      if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
      {
        throw new BadHttpRequestException(_localizer["Invalid tile"]);
      }

      // Check if tile exists
      var tile =
          await _dbContext.Tiles.Include(x => x.Users).SingleOrDefaultAsync(x => x.Id == tileId);
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
        (roles.Contains(UserRoles.EDITOR) && !tile.Users.Any(x => x.Id == user.Id)))
      {
        throw new BadHttpRequestException(_localizer["You are unable to edit this tile"]);
      }

      // Get all stops in selected tile + stops around that tile
      var stops = await _dbContext.Stops.Where(x =>
          x.Lon > tile.OverlapMinLon && x.Lon <= tile.OverlapMaxLon &&
          x.Lat > tile.OverlapMinLat && x.Lat <= tile.OverlapMaxLat).ToListAsync();

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
    public async Task<ActionResult<Tile>> GetUsers(string id)
    {
      Guid tileId;
      if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
      {
        throw new BadHttpRequestException(_localizer["Invalid tile"]);
      }

      List<ApplicationUser> allUsers = await _userManager.Users.ToListAsync();

      // Remove other users than editors
      List<ApplicationUser> editors = new List<ApplicationUser>();
      foreach (ApplicationUser user in allUsers)
      {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        // add roles to user
        user.Roles = roles;
        if (roles.Contains(UserRoles.EDITOR))
          editors.Add(user);
      }

      // Get current tile by id
      DbTile currentTile =
          await _dbContext.Tiles.Include(
              tile => tile.Users).SingleOrDefaultAsync(x => x.Id == tileId);

      TileWithUsers result = new TileWithUsers
      {
        Id = tileId,
        Users = new List<TileUser>()
      };

      foreach (ApplicationUser user in editors)
      {
        TileUser tileUser = new TileUser { Id = user.Id, UserName = user.UserName };
        result.Users.Add(tileUser);
        tileUser.IsAssigned = currentTile.Users.Any(x => x.Id == user.Id);
      }

      result.Users.Sort((x, y) => x.UserName.CompareTo(y.UserName));

      return Ok(result);

    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<string>> RemoveUser(string id)
    {
      DbTile currentTile =
          await _dbContext.Tiles.Include(
              tile => tile.Users).SingleOrDefaultAsync(x => x.Id == Guid.Parse(id));

      if (currentTile == null)
      {
        throw new BadHttpRequestException(_localizer["Unable to find tile"]);
      }

      if (currentTile.EditorApproved != null)
      {
        throw new BadHttpRequestException(_localizer["Unable to remove user from already approved tile"]);
      }

      currentTile.Users.Clear();
      _dbContext.SaveChanges();

      return Ok(_localizer["User successfully removed from the tile"]);
    }

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

      currentTile.Users.Clear();
      currentTile.Users.Add(user);

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
        if (currentTile.Users.Any(x => x.Id == user.Id))
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
      await SendEmails(currentTile);

      return Ok(_localizer["Tile approved"]);
    }

    private async Task SendEmails(DbTile currentTile)
    {
      List<User> usersInRole = new List<User>();
      if (User.IsInRole(UserRoles.EDITOR))
      {
        usersInRole = _mapper.Map<List<User>>(
            (List<ApplicationUser>)await _userManager.GetUsersInRoleAsync(UserRoles.SUPERVISOR));
      }
      else if (User.IsInRole(UserRoles.SUPERVISOR))
      {
        usersInRole = _mapper.Map<List<User>>(
            (List<ApplicationUser>)await _userManager.GetUsersInRoleAsync(UserRoles.ADMIN));
      }

      foreach (User user in usersInRole)
      {
        Task task = Task.Run(() => _emailService.SendEmailAsync(TileApprovedMessageBuilder(user, currentTile)));
      }
    }

    private MimeMessage TileApprovedMessageBuilder(User user, DbTile tile)
    {
      MimeMessage message = new MimeMessage();
      message.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
      message.To.Add(MailboxAddress.Parse(user.Email));
      message.Subject = _localizer["Tile approved"];

      BodyBuilder builder = new BodyBuilder();

      builder.TextBody = $@"{_localizer["Hello"]} {user.UserName},
{_localizer["One of the tiles was approved"]}
X: {tile.X}, Y: {tile.Y}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
      builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {user.UserName},</h3>
<p>{_localizer["One of the tiles was approved"]}</p><br/>
<p>X: {tile.X}, Y: {tile.Y}</p>
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>
      ";

      message.Body = builder.ToMessageBody();

      return message;
    }

    private async Task<DbTile> GetTileAsync(string tileId)
    {
      DbTile currentTile = await _dbContext.Tiles.Include(tile => tile.Users)
          .SingleOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
      if (currentTile == null)
      {
        throw new BadHttpRequestException(_localizer["Given tile does not exist"]);
      }
      return currentTile;
    }
  }
}
