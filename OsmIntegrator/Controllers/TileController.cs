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
            _mapper = mapper;
            _userManager = userManager;
            _roleManger = roleManager;
            _localizer = localizer;
            _emailService = emailService;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Tile>>> GetTiles()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            IList<string> roles = await _userManager.GetRolesAsync(user);

            List<DbTile> tiles;
            if (roles.Contains(UserRoles.ADMIN) || roles.Contains(UserRoles.SUPERVISOR))
            {
                tiles =
                    await _dbContext.Tiles.Include(x => x.Users).Where(
                        x => x.GtfsStopsCount > 0).ToListAsync();
                return Ok(_mapper.Map<List<Tile>>(tiles));
            }

            tiles = await _dbContext.Tiles.Include(x => x.Users).Where(x => x.GtfsStopsCount > 0).
                Where(x => x.Users.Any(x => x.Id == user.Id)).ToListAsync();

            return Ok(_mapper.Map<List<Tile>>(tiles));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
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
            if (!roles.Contains(UserRoles.SUPERVISOR) && !roles.Contains(UserRoles.ADMIN) &&
                roles.Contains(UserRoles.EDITOR) && !tile.Users.Any(x => x.Id == user.Id))
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

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            allUsers.RemoveAll(x => x.Id.Equals(currentUser.Id));

            // Remove other users than editors
            List<ApplicationUser> editors = new List<ApplicationUser>();
            foreach (ApplicationUser user in allUsers)
            {
                IList<string> roles = await _userManager.GetRolesAsync(user);
                // add roles to user
                user.Roles = roles;
                if (roles.Contains(UserRoles.EDITOR)) editors.Add(user);
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

            if (userRoles.Contains(UserRoles.SUPERVISOR))
            {
                currentTile.SupervisorApproved = user;
                currentTile.SupervisorApprovalTime = DateTime.Now;
            }
            else if (userRoles.Contains(UserRoles.EDITOR))
            {
                currentTile.EditorApproved = user;
                currentTile.EditorApprovalTime = DateTime.Now;
            }
            else
            {
                throw new BadHttpRequestException(_localizer["User doesn't contain required roles"]);
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

            string message = _localizer["One of the tiles was approved"];
            message += Environment.NewLine + $"X: {currentTile.X}, Y: {currentTile.Y}.";

            foreach (User user in usersInRole)
            {
                Task task = Task.Run(() => _emailService.SendEmailAsync(
                    user.Email, _localizer["Tile approved"],
                    message));
            }
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
