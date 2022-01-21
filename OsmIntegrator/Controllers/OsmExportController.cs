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
    public async Task<ActionResult<OsmChangeOutput>> GetExportChanges(Guid tileId)
    {
      DbTile tile = await _dbContext.Tiles
        .Include(x => x.Stops)
        .ThenInclude(x => x.GtfsConnections)
        .FirstOrDefaultAsync(x => x.Id == tileId);

      var comment = _osmExporter.GetComment(tile.X, tile.Y, byte.Parse(_configuration["ZoomLevel"], NumberFormatInfo.InvariantInfo));

      OsmChangeOutput output = new()
      {
        Changes = tile.GetUnexportedChanges(_localizer),
        Tags = _osmExporter.GetTags(comment)
      };

      return Ok(output);
    }

    [HttpPost("tiles/{tileId}/export")]
    [Authorize(Roles =
          UserRoles.EDITOR + "," +
          UserRoles.SUPERVISOR + "," +
          UserRoles.COORDINATOR + "," +
          UserRoles.ADMIN)]
    public async Task<ActionResult> Export(Guid tileId, [FromBody] OsmExportInput input)
    {
      DbTile tile = await _dbContext.Tiles
        .Include(x => x.Stops)
        .ThenInclude(x => x.OsmConnections)
        .Include(t => t.ExportReports)
        .FirstOrDefaultAsync(x => x.Id == tileId);
      string osmChangeFile = _osmExporter.GetOsmChangeFile(tile);

      tile.ExportReports.Add(new DbTileExportReport
      {
        CreatedAt = DateTime.Now,
        TileReport = new()
        {
          TileId = tileId,
          TileX = tile.X,
          TileY = tile.Y
        }
      });

      await _dbContext.SaveChangesAsync();

      // Use python script to send osmchange.osc to OSM
      // Link to upload.py: https://github.com/grigory-rechistov/osm-bulk-upload

      return Ok();
    }

    [HttpGet("tiles/{tileId}/export/osc")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult> GetExportFile(Guid tileId)
    {
      DbTile tile = await _dbContext.Tiles
        .AsNoTracking()
        .Include(t => t.Stops).ThenInclude(s => s.OsmConnections).ThenInclude(c => c.GtfsStop)
        .FirstOrDefaultAsync(x => x.Id == tileId);

      string filecontent = _osmExporter.GetOsmChangeFile(tile);

      return File(Encoding.UTF8.GetBytes(filecontent), "text/xml", "osmchange.osc");
    }
  }
}