using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Roles;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Services;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.Database.Models;
using System;

namespace OsmIntegrator.Controllers
{
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OsmExportController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<OsmExportController> _logger;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<OsmExportController> _localizer;
        private readonly IOsmExporter _osmExporter;

        public OsmExportController(
            ApplicationDbContext dbContext,
            ILogger<OsmExportController> logger,
            IMapper mapper,
            IStringLocalizer<OsmExportController> localizer,
            IOsmExporter osmExporter)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
            _localizer = localizer;
            _osmExporter = osmExporter;
        }

        [HttpGet("{tileId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult<string>> GetChangeFile(string tileId)
        {
          DbTile tile = await _dbContext.Tiles.FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
          string osmChangeFile = await _osmExporter.GetOsmChangeFile(tile);
          return Ok(osmChangeFile);
        }
    }
}