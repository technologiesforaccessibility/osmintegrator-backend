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
        [HttpPut()]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<IActionResult> Add([FromBody] ConnectionAction connectionAction)
        {
            try
            {
                Error validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                DbStop gtfsStop = await _dbContext.Stops
                    .Include(x => x.Connections).FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);
                if (gtfsStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no GTFS stop with id {connectionAction.GtfsStopId}." });
                }

                DbStop osmStop = await _dbContext.Stops
                    .Include(x => x.Connections).FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);
                if (osmStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no OSM stop with id {connectionAction.OsmStopId}." });
                }

                ApplicationUser currentUser = await _userManager.GetUserAsync(User);
                List<DbConnection> existingConnections = await _dbContext.Connections
                    .Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                    x.GtfsStopId == connectionAction.GtfsStopId).ToListAsync();

                // There is an imported and added by user connection
                if (existingConnections.Count == 2)
                {
                    return BadRequest(new ValidationError()
                    {
                        Message = $"Overwritten connection already exists between osm: {connectionAction.OsmStopId} and gtfs: {connectionAction.GtfsStopId} stop."
                    });
                }
                // There is connection added by user or imported connection
                else if (existingConnections.Count == 1)
                {
                    DbConnection connection = existingConnections.First();

                    // Imported connection wasn't removed by the user
                    if (connection.Imported && !connection.Removed)
                    {
                        return BadRequest(new ValidationError()
                        {
                            Message = $"Cannot overwrite imported and not removed connection for osm: {connectionAction.OsmStopId} and gtfs: {connectionAction.GtfsStopId} stop."
                        });
                    }
                    // The connection has already been created by the user
                    else if (!connection.Imported)
                    {
                        return BadRequest(new ValidationError()
                        {
                            Message = $"Connection already exists between osm: {connectionAction.OsmStopId} and gtfs: {connectionAction.GtfsStopId} stop."
                        });
                    }

                    connection.Removed = false;
                }
                else
                {
                    // No connection was established between osm and gtfs stop - neighter
                    // imported nor created by the user.
                    DbConnection newConnection = new DbConnection()
                    {
                        OsmStop = osmStop,
                        GtfsStop = gtfsStop,
                        User = currentUser,
                        Imported = false,
                        Removed = false
                    };
                }
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
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

                List<DbConnection> connections = await _dbContext.Connections.Include(x => x.OsmStop)
                    .Where(x => x.OsmStop.TileId == Guid.Parse(id)).ToListAsync();

                List<Connection> result = _mapper.Map<List<Connection>>(connections);

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
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