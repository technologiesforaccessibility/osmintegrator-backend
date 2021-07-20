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
using System.Net;

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

        public ConnectionsController(ApplicationDbContext dbContext,
            ILogger<ConnectionsController> logger, IMapper mapper,
            UserManager<ApplicationUser> userManager,
            ITileValidator tileValidator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _tileValidator = tileValidator;
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

            // Check if connection already exists and has not been deleted.
            List<DbStopLink> existingConnections = await _dbContext.Connections
                .Where(x => x.OsmStopId == connectionAction.OsmStopId &&
                        x.GtfsStopId == connectionAction.GtfsStopId)
                .OrderByDescending(link => link.CreatedAt)
                .ToListAsync();

            DbStopLink existingConnection = existingConnections.FirstOrDefault();
            bool imported = false;
            if (existingConnection != null)
            {
                if (existingConnection.OperationType == ConnectionOperationType.Added)
                {
                    string message = "The connection already exists." +
                        $"OSM stop id: {connectionAction.OsmStopId}, GTFS stop id: {connectionAction.GtfsStopId}.";
                    throw new BadHttpRequestException(message);
                }
                // Check wether last connection was imported
                imported = existingConnection.Imported;
            }

            // Check if GTFS stop has already been connected to another stop.
            DbStop gtfsStop = await _dbContext.Stops
                .Include(x => x.StopLinks)
                .FirstOrDefaultAsync(x => x.Id == connectionAction.GtfsStopId);

            if (gtfsStop == null)
            {
                throw new BadHttpRequestException($"There is no GTFS stop with id {connectionAction.GtfsStopId}.");
            }

            DbStopLink gtfsConnection = gtfsStop.StopLinks
                .OrderByDescending(link => link.CreatedAt)
                .FirstOrDefault();

            if (gtfsConnection != null && gtfsConnection.OperationType == ConnectionOperationType.Added)
            {
                throw new BadHttpRequestException($"The GTFS stop has already been connected with different stop. Stop id: {connectionAction.GtfsStopId}.");
            }

            // Check if OSM stop has already been connected to another stop.
            DbStop osmStop = await _dbContext.Stops
                .Include(x => x.StopLinks)
                .FirstOrDefaultAsync(x => x.Id == connectionAction.OsmStopId);

            if (osmStop == null)
            {
                throw new BadHttpRequestException($"There is no OSM stop with id {connectionAction.OsmStopId}.");
            }

            DbStopLink osmConnection = osmStop.StopLinks
                .OrderByDescending(link => link.CreatedAt)
                .FirstOrDefault();

            if (osmConnection != null && osmConnection.OperationType == ConnectionOperationType.Added)
            {
                throw new BadHttpRequestException($"The OSM stop has already been connected with different stop. Stop id: {connectionAction.OsmStopId}.");
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

        [HttpDelete()]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<IActionResult> Remove([FromBody] ConnectionAction connectionAction)
        {
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
                    throw new BadHttpRequestException(message);
                }
                // Check wether last connection was imported
                imported = existingConnection.Imported;
            }
            else if (existingConnection == null)
            {
                string message = "Connot remove connection which doesn't exist. " +
                        $"OSM stop id: {connectionAction.OsmStopId}, GTFS stop id: {connectionAction.GtfsStopId}.";
                throw new BadHttpRequestException(message);
            }

            ApplicationUser currentUser = await _userManager.GetUserAsync(User);

            DbStopLink newConnection = new DbStopLink()
            {
                OsmStopId = (Guid)connectionAction.OsmStopId,
                GtfsStopId = (Guid)connectionAction.GtfsStopId,
                User = currentUser,
                Imported = imported,
                OperationType = ConnectionOperationType.Removed
            };

            _dbContext.Connections.Add(newConnection);
            _dbContext.SaveChanges();

            return Ok("Connection successfully removed!");
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

            Error error = await _tileValidator.Validate(_dbContext, id);
            if (error != null) throw new BadHttpRequestException(error.Message);

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

        /// <summary>
        /// Get all connections.
        /// </summary>
        /// <returns>All existing and not existing connections.</returns>
        [HttpGet()]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Connection>>> GetAll()
        {
            List<DbStopLink> connections = await _dbContext.Connections.FromSqlRaw(
                "SELECT DISTINCT ON (\"GtfsStopId\", \"OsmStopId\") * " +
                "FROM \"StopLinks\" c " +
                "ORDER BY \"GtfsStopId\", \"OsmStopId\", \"CreatedAt\" DESC"
            ).ToListAsync();
            List<Connection> result = _mapper.Map<List<Connection>>(connections);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR)]
        public async Task<ActionResult<string>> Approve(string id)
        {

            DbStopLink link = await _dbContext.Connections.Where(c => c.Id == Guid.Parse(id)).FirstOrDefaultAsync();
            if (link == null) {
                throw new BadHttpRequestException($"Connection with id: {id} does not exists");
            }
            ApplicationUser currentUser = await _userManager.GetUserAsync(User);
            link.ApprovedBy = currentUser;
            _dbContext.SaveChanges();

            return Ok("Connection approved");
        }
    }


}