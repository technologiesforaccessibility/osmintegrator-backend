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

        public TileController(
            ILogger<TileController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<Tile>>> GetAllTiles()
        {
            try
            {
                List<DbTile> result = await _dbContext.Tiles.Where(
                        x => x.GtfsStopsCount > 0).ToListAsync();
                return Ok(_mapper.Map<List<Tile>>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetAllTiles)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tile>> GetStops(string id)
        {
            try
            {
                Guid guidId = Guid.Parse(id);
                var result = await _dbContext.Tiles.SingleOrDefaultAsync(x => x.Id == guidId);

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
                List<ApplicationUser> allUsers = await _userManager.Users.ToListAsync();
                ApplicationUser currentUser = await _userManager.GetUserAsync(User);
                allUsers.RemoveAll(x => x.Id.Equals(currentUser.Id));

                List<ApplicationUser> editors = new List<ApplicationUser>();
                foreach(ApplicationUser user in allUsers)
                {
                    IList<string> roles = await _userManager.GetRolesAsync(user);
                    if(roles.Contains(UserRoles.EDITOR)) editors.Add(user);
                }

                DbTile currentTile =
                    await _dbContext.Tiles.Include(
                        tile => tile.Users).SingleOrDefaultAsync(x => x.Id == Guid.Parse(id));

                TileWithUsers result = new TileWithUsers
                {
                    Id = Guid.Parse(id),
                    Users = new List<TileUser>()
                };

                foreach (ApplicationUser user in editors)
                {
                    TileUser tileUser = new TileUser { Id = user.Id };
                    result.Users.Add(tileUser);
                    tileUser.IsAssigned = currentTile.Users.Any(x => x.Id == user.Id);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetUsers)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<Tile>> UpdateUsers(string id)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(GetUsers)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tile>> GetAssignedUsers(string id)
        {
            try
            {
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
