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
using OsmIntegrator.Extensions;
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

        public TileController(
            ILogger<TileController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IValidationHelper validationHelper
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
            _validationHelper = validationHelper;
        }

        [HttpGet]
        public async Task<ActionResult<List<Tile>>> GetAllTiles()
        {
            try
            {
                List<DbTile> tiles = await _dbContext.Tiles.Include(x => x.Users).Where(
                        x => x.GtfsStopsCount > 0).ToListAsync();

                List<Tile> result = _mapper.Map<List<Tile>>(tiles);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetAllTiles)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<Tile>> GetStops(string id)
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
                var result = await _dbContext.Tiles.SingleOrDefaultAsync(x => x.Id == tileId);

                if (result == null)
                {
                    Error error = new Error();
                    error.Title = $"Unable to find tile with id {id}.";
                    error.Message = "General error";
                    return BadRequest(error);
                }

                var stops = await _dbContext.Stops.Where(x =>
                    x.Lon > result.OverlapMinLon && x.Lon <= result.OverlapMaxLon &&
                    x.Lat > result.OverlapMinLat && x.Lat <= result.OverlapMaxLat).ToListAsync();

                foreach (DbStop stop in stops)
                {
                    if (stop.Lon > result.MinLon && stop.Lon <= result.MaxLon &&
                        stop.Lat > result.MinLat && stop.Lat <= result.MaxLat)
                    {
                        stop.OutsideSelectedTile = false;
                        continue;
                    }
                    stop.OutsideSelectedTile = true;
                }

                result.Stops = stops;

                return Ok(_mapper.Map<Tile>(result));
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

                // Get all users with roles and remove current one
                List<ApplicationUser> allUsers = await _userManager.Users.Include(x => x.UserRoles).
                    ThenInclude(x => x.Role).ToListAsync();
                ApplicationUser currentUser = await _userManager.GetUserAsync(User);
                allUsers.RemoveAll(x => x.Id.Equals(currentUser.Id));

                // Remove other users than editors
                List<ApplicationUser> editors = new List<ApplicationUser>();
                foreach (ApplicationUser user in allUsers)
                {
                    IList<string> roles = user.GetRoles();
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
