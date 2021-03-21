using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels.Errors;
using System.Collections.Generic;
using AutoMapper;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Roles;
using Microsoft.AspNetCore.Authorization;
using OsmIntegrator.Tools;

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
        private readonly IValidationHelper _validationHelper;

        private readonly RoleManager<ApplicationRole> _roleManger;

        public TileController(
            ILogger<TileController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IValidationHelper validationHelper,
            RoleManager<ApplicationRole> roleManager
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
            _validationHelper = validationHelper;
            _roleManger = roleManager;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Tile>>> GetTiles()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetTiles)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<Stop>> GetStops(string id)
        {
            try
            {
                // Validate tile id
                Guid tileId;
                if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
                {
                    return BadRequest(new ValidationError
                    {
                        Message = "Unable to validate tile id."
                    });
                }

                // Check if tile exists
                var tile =
                    await _dbContext.Tiles.Include(x => x.Users).SingleOrDefaultAsync(x => x.Id == tileId);
                if (tile == null)
                {
                    Error error = new Error();
                    error.Title = $"Unable to find tile with id {id}.";
                    error.Message = "General error";
                    return BadRequest(error);
                }

                // Get current user roles
                ApplicationUser user = await _userManager.GetUserAsync(User);
                IList<string> roles = await _userManager.GetRolesAsync(user);

                // Check if user is assigned to a tile?
                // When user is SUPERVISOR or ADMIN this validation is not required.
                if (!roles.Contains(UserRoles.SUPERVISOR) && !roles.Contains(UserRoles.ADMIN) &&
                    roles.Contains(UserRoles.EDITOR) && !tile.Users.Any(x => x.Id == user.Id))
                {
                    return BadRequest(new ValidationError
                    {
                        Message = "Current user is not able to edit this tile."
                    });
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetStops)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<Tile>> GetUsers(string id)
        {
            try
            {
                Guid tileId;
                if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
                {
                    return BadRequest(new ValidationError
                    {
                        Message = "Unable to validate tile id."
                    });
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
                    user.Roles  = roles;
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetUsers)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }

        [HttpPost()]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<Tile>> UpdateUsers([FromBody] TileWithUsers tileWithUsers)
        {
            try
            {
                var validationResult = _validationHelper.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                // Get current tile by id
                DbTile currentTile =
                    await _dbContext.Tiles.Include(
                        tile => tile.Users).SingleOrDefaultAsync(x => x.Id == tileWithUsers.Id);

                foreach (TileUser user in tileWithUsers.Users)
                {
                    ApplicationUser foundUser = currentTile.Users.FirstOrDefault(x => x.Id == user.Id);
                    if (user.IsAssigned && foundUser == null)
                    {
                        ApplicationUser toAddUser =
                            await _userManager.Users.SingleOrDefaultAsync(x => x.Id == user.Id);

                        if (toAddUser != null)
                        {
                            currentTile.Users.Add(toAddUser);
                        }
                        else
                        {
                            return BadRequest(new Error
                            {
                                Title = "Missing user",
                                Message = $"There is no user with id: {user.Id} and user name: {user.UserName}."
                            });
                        }
                    }
                    else if (!user.IsAssigned && foundUser != null)
                    {
                        currentTile.Users.Remove(foundUser);
                    }
                }

                _dbContext.SaveChanges();

                return Ok("Users successfully assigned or removed from tile.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(UpdateUsers)} method.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tile>> GetAssignedUsers(string id)
        {
            try
            {
                Guid tileId;
                if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out tileId))
                {
                    return BadRequest(new ValidationError
                    {
                        Message = "Unable to validate tile id."
                    });
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetUsers)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }
    }
}
