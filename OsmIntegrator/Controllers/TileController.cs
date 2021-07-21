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
using OsmIntegrator.Roles;
using OsmIntegrator.Validators;

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

        public TileController(
            ILogger<TileController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IStringLocalizer<TileController> localizer
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
            _roleManger = roleManager;
            _localizer = localizer;
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
        public async Task<ActionResult<string>> UpdateUser(string id, [FromBody] User u)
        {


            var user = await PrepareUserForTile(id, u);
            IList<string> roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRoles.EDITOR))
            {

                throw new BadHttpRequestException(_localizer["You are not an editor"]);
            }

            DbTile currentTile =
            await _dbContext.Tiles
                .Include(tile => tile.Users)
                .SingleOrDefaultAsync(x => x.Id == Guid.Parse(id));


            currentTile.Users.Clear();
            currentTile.Users.Add(user);

            _dbContext.SaveChanges();

            return Ok(_localizer["Tile approved"]);


        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.EDITOR)]
        public async Task<ActionResult<string>> Approve(string id, [FromBody] User u)
        {

            DbTile currentTile = await _dbContext.Tiles
                .Include(tile => tile.Approvers)
                .SingleOrDefaultAsync(x => x.Id == Guid.Parse(id));

            var user = await PrepareUserForTile(id, u);
            currentTile.Approvers.Add(user);
            _dbContext.SaveChanges();

            return Ok(_localizer["Tile approved"]);
        }
        private async Task<ApplicationUser> PrepareUserForTile(string id, User u)
        {

            ApplicationUser selectedUser = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == u.Id);

            if (selectedUser == null)
            {
                throw new BadHttpRequestException(_localizer["Given user does not exist"]);
            }

            // Get current tile by id
            DbTile currentTile =
                await _dbContext.Tiles.Include(
                    tile => tile.Users).SingleOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (currentTile == null)
            {
                throw new BadHttpRequestException(_localizer["Given tile does not exist"]);
            }
            return selectedUser;
        }
    }
}
