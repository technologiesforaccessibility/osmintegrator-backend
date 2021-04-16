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
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Roles;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Validators;
using OsmIntegrator.Enums;

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
        private readonly IModelValidator _modelValidator;

        public ConnectionsController(ApplicationDbContext dbContext,
            ILogger<ConnectionsController> logger, IMapper mapper,
            UserManager<ApplicationUser> userManager,
            ITileValidator tileValidator,
            IModelValidator modelValidator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _tileValidator = tileValidator;
            _modelValidator = modelValidator;
        }

        /// <summary>
        /// Add/remove a connection for specific user.
        /// 
        /// Validation:
        ///     1) connection is between two same objects
        ///     2) one or both objects doesn't exist
        ///     3) cannot make connection if one of the stop is already connected
        /// 
        /// There are following combinations:
        /// I. Add
        ///     1) add new connection
        ///     2) add new connection in place of removed imported connection
        ///     3) add new connection and this connection already exists
        ///     4) add new connection in place of existing imported connection
        /// 
        /// II. Remove
        ///     1) remove connection added by user
        ///     2) remove connection which doesn't exist
        ///     3) remove imported connection
        ///     4) remove already removed imported connection
        /// </summary>
        /// <param name="id">Tile id</param>
        /// <param name="connectionAction">Add or remove connection action</param>
        /// <returns>Generic result or error.</returns>
        [HttpPost("{id}")]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<IActionResult> Add(string id, [FromBody] ConnectionAction connectionAction)
        {
            try
            {
                Error validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                Error error = await _tileValidator.Validate(_dbContext, id);
                if (error != null) return BadRequest(error);

                var currentUser = await _userManager.GetUserAsync(User);

                DbConnection existingConnection = await _dbContext.Connections.FirstOrDefaultAsync(
                    x => x.OsmStopId == connectionAction.OsmStopId && x.GtfsStopId == connectionAction.GtfsStopId
                );
                if(existingConnection == null)
                {
                    
                }

                DbTile tile = await _dbContext.Tiles
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (connectionAction.Type == ConnectionType.Added)
                {
                    DbConnection connection = tile.Connections
                        .FirstOrDefault(x => x.OsmStopId == connectionAction.OsmStopId &&
                         x.GtfsStopId == connectionAction.GtfsStopId);
                    if (connection != null)
                    {
                        return BadRequest(new ValidationError() { Message = "Connection already exists" });
                    }
                    DbConnection newConnection = new DbConnection()
                    {
                        GtfsStopId = connectionAction.GtfsStopId,
                        OsmStopId = connectionAction.OsmStopId,
                        User = currentUser,
                        Imported = false,
                        Tile = tile
                    };
                }
                else if (connectionAction.Type == ConnectionType.Removed)
                {

                }

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request. Id: {id}.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        /// <summary>
        /// Get connection for tile id.
        /// </summary>
        /// <param name="id">Tile id.</param>
        /// <returns>Collection of connections in selected tile.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Connection>>> Get(string id)
        {
            try
            {
                Error error = await _tileValidator.Validate(_dbContext, id);
                if (error != null) return BadRequest(error);

                DbTile tile = await _dbContext.Tiles
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                List<Connection> result = _mapper.Map<List<Connection>>(tile.Connections);

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request. Id: {id}.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        /// <summary>
        /// Get all connections.
        /// </summary>
        /// <returns>All existing and not existing connections.</returns>
        [HttpGet()]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Connection>>> GetAll()
        {
            try
            {
                List<DbConnection> connections = await _dbContext.Connections.ToListAsync();
                List<Connection> result = _mapper.Map<List<Connection>>(connections);
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }
    }
}