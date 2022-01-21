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
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace OsmIntegrator.Controllers
{
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [Produces(MediaTypeNames.Application.Json)]
  [EnableCors("AllowOrigin")]
  [Route("api")]
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

    [HttpGet("tiles/{tileId}/export/changes")]
        [Authorize(Roles = 
          UserRoles.EDITOR + "," + 
          UserRoles.SUPERVISOR + "," + 
          UserRoles.COORDINATOR + "," + 
          UserRoles.ADMIN)]
    {
      
          DbTile tile = await _dbContext.Tiles
            .Include(x => x.Stops)
            .ThenInclude(x => x.OsmConnections)
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));
          string osmChangeFile = _osmExporter.GetOsmChangeFile(tile);

      OsmExportInitialInfo output = new()
      {
        Changes = await _osmExporter.GetOsmChangeFile(tile),
        Tags = _osmExporter.GetTags(),
            Comment = _osmExporter.GetComment(tile.X, tile.Y, byte.Parse(_configuration["ZoomLevel"], NumberFormatInfo.InvariantInfo))
      };

      return Ok(output);
    }

    [HttpPost("tiles/{tileId}/export")]
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

      // Use python script to send osmchange.osc to OSM
      // Link to upload.py: https://github.com/grigory-rechistov/osm-bulk-upload

      return Ok();
    }

    [HttpGet("tiles/{tileId}/export/osc")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult> GetExportFile(string tileId)
    {
      DbTile tile = await _dbContext.Tiles.FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));

      string filecontent = await _osmExporter.GetOsmChangeFile(tile);

      return File(Encoding.UTF8.GetBytes(filecontent), "text/plain", "osmchange.osc");
    }
  }
}