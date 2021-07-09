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

                // Check if connection already exists and has not been deleted.
                List<DbStopLink> existingConnections = await _dbContext.Connections.Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                    x.GtfsStopId == connectionAction.GtfsStopId).ToListAsync();
                DbStopLink existingConnection = existingConnections.LastOrDefault();
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

                // Check if GTFS stop has already been connected to another stop.
                DbStop gtfsStop = await _dbContext.Stops
                    .Include(x => x.StopLinks).FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);
                if (gtfsStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no GTFS stop with id {connectionAction.GtfsStopId}." });
                }
                DbStopLink gtfsConnection = gtfsStop.StopLinks.LastOrDefault();
                if (gtfsConnection != null && gtfsConnection.OperationType == ConnectionOperationType.Added)
                {
                    string message = $"The GTFS stop has already been connected with different stop. Stop id: {connectionAction.GtfsStopId}.";
                    return BadRequest(new ValidationError() { Message = message });
                }

                // Check if OSM stop has already been connected to another stop.
                DbStop osmStop = await _dbContext.Stops
                    .Include(x => x.StopLinks).FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);
                if (osmStop == null)
                {
                    return BadRequest(new ValidationError() { Message = $"There is no OSM stop with id {connectionAction.OsmStopId}." });
                }
                DbStopLink osmConnection = osmStop.StopLinks.LastOrDefault();
                if (osmConnection != null && osmConnection.OperationType == ConnectionOperationType.Added)
                {
                    string message = $"The OSM stop has already been connected with different stop. Stop id: {connectionAction.OsmStopId}.";
                    return BadRequest(new ValidationError() { Message = message });
                }

                ApplicationUser currentUser = await _userManager.GetUserAsync(User);

                DbStopLink newConnection = new DbStopLink()
                {
                    OsmStop = osmStop,
                    GtfsStop = gtfsStop,
                    User = currentUser,
                    Imported = imported,
                    OperationType = ConnectionOperationType.Added
                };

                _dbContext.Connections.Add(newConnection);
                _dbContext.SaveChanges();

                return Ok("Connection successfully added!");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        [HttpDelete()]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<IActionResult> Remove([FromBody] ConnectionAction connectionAction)
        {
            try
            {
                Error validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                List<DbStopLink> existingConnections = await _dbContext.Connections.Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                    x.GtfsStopId == connectionAction.GtfsStopId).ToListAsync();
                DbStopLink existingConnection = existingConnections.LastOrDefault();
                bool imported = false;
                if (existingConnection != null)
                {
                    if (existingConnection.OperationType == ConnectionOperationType.Removed)
                    {
                        string message = "The connection have already been removed. " +
                            $"OSM stop id: {connectionAction.OsmStopId}, GTFS stop id: {connectionAction.GtfsStopId}.";
                        return BadRequest(new ValidationError() { Message = message });
                    }
                    // Check wether last connection was imported
                    imported = existingConnection.Imported;
                }
                else if (existingConnection == null)
                {
                    string message = "Connot remove connection which doesn't exist. " +
                            $"OSM stop id: {connectionAction.OsmStopId}, GTFS stop id: {connectionAction.GtfsStopId}.";
                    return BadRequest(new ValidationError() { Message = message });
                }

                ApplicationUser currentUser = await _userManager.GetUserAsync(User);

                DbStopLink newConnection = new DbStopLink()
                {
                    OsmStopId = connectionAction.OsmStopId,
                    GtfsStopId = connectionAction.GtfsStopId,
                    User = currentUser,
                    Imported = imported,
                    OperationType = ConnectionOperationType.Removed
                };

                _dbContext.Connections.Add(newConnection);
                _dbContext.SaveChanges();

                return Ok("Connection successfully removed!");
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

                string query = 
                    "SELECT DISTINCT ON (\"GtfsStopId\", \"OsmStopId\") * " +
                    "FROM \"StopLinks\" c " +
                    "ORDER BY \"GtfsStopId\", \"OsmStopId\", \"CreatedAt\" DESC";

                List<DbStopLink> connections = await _dbContext.Connections.FromSqlRaw(
                    query).Include(x => x.OsmStop).ToListAsync();

                connections = connections.Where(
                    x => x.OsmStop.TileId == Guid.Parse(id) && x.OperationType != ConnectionOperationType.Removed)
                    .ToList();

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
                List<DbStopLink> connections = await _dbContext.Connections.FromSqlRaw(
                    "SELECT DISTINCT ON (\"GtfsStopId\", \"OsmStopId\") * " +
                    "FROM \"StopLinks\" c " +
                    "ORDER BY \"GtfsStopId\", \"OsmStopId\", \"CreatedAt\" DESC"
                ).ToListAsync();
                List<Connection> result = _mapper.Map<List<Connection>>(connections);
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR)]
        public async Task<ActionResult<string>> Approve(string id)
        {
            try
            {                                                
                DbStopLink link = await _dbContext.Connections.Where(c => c.Id == Guid.Parse(id)).FirstOrDefaultAsync();
                if (link == null) {
                    return BadRequest(new Error() {
                        Title = $"Connection with id: {id} does not exists" 
                    });
                }
                link.Approved = true;                                                        
                _dbContext.SaveChanges();

                return Ok("Connection approved");                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Cannot approve connection {id}");
                return Problem("Cannot approve connection");
            }
        }        
    }
}