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

                // Check if GTFS stop has already been connected to another stop.
                DbStop gtfsStop = await _dbContext.Stops
                    .Include(x => x.Connections).FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);
                if (gtfsStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no GTFS stop with id {connectionAction.GtfsStopId}." });
                }
                DbConnection gtfsConnection = gtfsStop.Connections.LastOrDefault();
                if (gtfsConnection != null && gtfsConnection.OperationType == ConnectionOperationType.Added)
                {
                    string message = $"The GTFS stop has already been connected with different stop. Stop id: {connectionAction.GtfsStopId}.";
                    return BadRequest(new ValidationError() { Message = message });
                }

                // Check if OSM stop has already been connected to another stop.
                DbStop osmStop = await _dbContext.Stops
                    .Include(x => x.Connections).FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);
                if (osmStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no OSM stop with id {connectionAction.OsmStopId}." });
                }
                DbConnection osmConnection = osmStop.Connections.LastOrDefault();
                if (osmConnection != null && osmConnection.OperationType == ConnectionOperationType.Added)
                {
                    string message = $"The OSM stop has already been connected with different stop. Stop id: {connectionAction.OsmStopId}.";
                    return BadRequest(new ValidationError() { Message = message });
                }




                // Check if connection already exists and has not been deleted.
                DbConnection existingConnection = await _dbContext.Connections.Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                    x.GtfsStopId == connectionAction.GtfsStopId).LastOrDefaultAsync();
                bool imported = false;
                if (existingConnection != null)
                {
                    if (existingConnection.OperationType == ConnectionOperationType.Added)
                    {
                        string message = "The connection already exists." +
                            $"OSM stop id: {connectionAction.OsmStopId}, GTFS stop id: {connectionAction.GtfsStopId}.";
                        return BadRequest(new ValidationError() { Message = message });
                    }
                    // Check wether last connection was imported
                    imported = existingConnection.Imported;
                }

                ApplicationUser currentUser = await _userManager.GetUserAsync(User);

                DbConnection newConnection = new DbConnection()
                {
                    OsmStop = osmStop,
                    GtfsStop = gtfsStop,
                    User = currentUser,
                    Imported = imported,
                    OperationType = ConnectionOperationType.Added
                };

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