using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.Roles;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.Database.Models;
using System;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace OsmIntegrator.Controllers
{
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces(MediaTypeNames.Application.Json)]
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
        private readonly IConfiguration _configuration;

        public OsmExportController(
            ApplicationDbContext dbContext,
            ILogger<OsmExportController> logger,
            IMapper mapper,
            IStringLocalizer<OsmExportController> localizer,
            IOsmExporter osmExporter,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
            _localizer = localizer;
            _osmExporter = osmExporter;
            _configuration = configuration;
        }

        [HttpGet("{tileId}")]
        [Authorize(Roles = 
          UserRoles.EDITOR + "," + 
          UserRoles.SUPERVISOR + "," + 
          UserRoles.COORDINATOR + "," + 
          UserRoles.ADMIN)]
        public async Task<ActionResult<OsmChangeOutput>> GetChangeFile(string tileId)
        {
          DbTile tile = await _dbContext.Tiles
            .Include(x => x.Stops)
            .ThenInclude(x => x.OsmConnections)
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
          string osmChangeFile = _osmExporter.GetOsmChangeFile(tile);

          OsmChangeOutput output = new()
          {
            OsmChangeFileContent = osmChangeFile,
            Comment = _osmExporter.GetComment(
              tile.X, tile.Y, byte.Parse(_configuration["ZoomLevel"], NumberFormatInfo.InvariantInfo))
          };

          return Ok(output);
        }

        [HttpPost("{tileId}")]
        [Authorize(Roles = 
          UserRoles.EDITOR + "," + 
          UserRoles.SUPERVISOR + "," + 
          UserRoles.COORDINATOR + "," + 
          UserRoles.ADMIN)]
        public async Task<ActionResult> Export(string tileId, [FromBody] OsmExportInput input)
        {
          DbTile tile = await _dbContext.Tiles
            .Include(x => x.Stops)
            .ThenInclude(x => x.OsmConnections)
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
          string osmChangeFile = _osmExporter.GetOsmChangeFile(tile);

          // TODO: Upload to the OSM

          return Ok();
        }
    }
}