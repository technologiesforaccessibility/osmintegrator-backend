using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using Microsoft.AspNetCore.Authorization;
using OsmIntegrator.ApiModels.Errors;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Roles;
using Microsoft.Extensions.Localization;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    public class StopController : ControllerBase
    {
        private readonly ILogger<StopController> _logger;
        private readonly ApplicationDbContext _dbContext;

        private readonly IMapper _mapper;
        private readonly IStringLocalizer<StopController> _localizer;

        public StopController(
            ILogger<StopController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            IStringLocalizer<StopController> localizer
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<List<Stop>>> Get()
        {
            var result = await _dbContext.Stops.ToListAsync();
            return Ok(_mapper.Map<List<Stop>>(result));
        }
    }
}
