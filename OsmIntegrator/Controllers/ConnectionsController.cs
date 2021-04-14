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

        private readonly IMapper _mapper;

        public ConnectionsController(ApplicationDbContext dbContext,
            ILogger<ConnectionsController> logger, IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
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
                if (string.IsNullOrEmpty(id))
                {
                    string message = $"Id cannot be null.";
                    return BadRequest(new ValidationError() { Message = message });
                }

                DbTile tile = await _dbContext.Tiles
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (tile == null)
                {
                    string message = $"No tile with id {id}.";
                    return BadRequest(new ValidationError() { Message = message });
                }

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