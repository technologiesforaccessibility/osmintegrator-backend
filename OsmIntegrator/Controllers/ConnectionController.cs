using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Roles;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    public class ConnectionController : ControllerBase
    {
        private readonly ILogger<ConnectionController> _logger;

        [HttpGet("{id}")]
        [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Connection>>> Get(string id)
        {
            try
            {
                

                List<Connection> result = new List<Connection>();

                return Ok(result);

            } catch(Exception e)
            {
                return BadRequest();
            }
        }
    }
}