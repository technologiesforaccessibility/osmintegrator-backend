using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Tools;
using OsmIntegrator.OsmApi;
using OsmIntegrator.Extensions;
using OsmIntegrator.Validators;
using OsmIntegrator.ApiModels.OsmExport;
using System.Collections.Generic;
using System.Linq;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Interfaces;

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
    private readonly IStringLocalizer<OsmExportController> _localizer;
    private readonly IOsmExporter _osmExporter;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOsmExportBuilder _osmExportBuilder;
    private readonly IExternalServicesConfiguration _externalServices;
    private readonly ITileExportValidator _tileExportValidator;

    public OsmExportController(
        ApplicationDbContext dbContext,
        ILogger<OsmExportController> logger,
        IStringLocalizer<OsmExportController> localizer,
        IOsmExporter osmExporter,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        IOsmExportBuilder osmExportBuilder,
        IExternalServicesConfiguration externalServices,
        ITileExportValidator tileExportValidator)
    {
      _dbContext = dbContext;
      _logger = logger;
      _localizer = localizer;
      _osmExporter = osmExporter;
      _configuration = configuration;
      _userManager = userManager;
      _osmExportBuilder = osmExportBuilder;
      _externalServices = externalServices;
      _tileExportValidator = tileExportValidator;
    }

    [HttpGet("tiles/{tileId}/export/changes")]
    [Authorize(Roles =
          UserRoles.EDITOR + "," +
          UserRoles.SUPERVISOR + "," +
          UserRoles.COORDINATOR + "," +
          UserRoles.ADMIN)]
    public async Task<ActionResult<OsmChangeOutput>> GetExportChanges(Guid tileId)
    {
      if (!await _tileExportValidator.ValidateDelayAsync(tileId))
      {
        throw new BadHttpRequestException(_localizer["Delay required"]);
      }

      if (!await _tileExportValidator.ValidateVersionAsync(tileId))
      {
        throw new BadHttpRequestException(_localizer["Import required"]);
      }

      DbTile tile = await _dbContext.Tiles
        .Include(x => x.Stops.Where(s => s.StopType == StopType.Gtfs))
        .ThenInclude(x => x.GtfsConnections)
        .ThenInclude(x => x.OsmStop)
        .FirstOrDefaultAsync(x => x.Id == tileId);

      if(tile == null)
        throw new BadHttpRequestException(_localizer["Selected tile doesn't exist"]);
      
      string comment = 
        _osmExporter.GetComment(tile.X, tile.Y, 
          byte.Parse(_configuration["ZoomLevel"],
            NumberFormatInfo.InvariantInfo));

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
      if (!await _tileExportValidator.ValidateDelayAsync(tileId))
      {
        throw new BadHttpRequestException(_localizer["Delay required"]);
      }

      if (!await _tileExportValidator.ValidateVersionAsync(tileId))
      {
        throw new BadHttpRequestException(_localizer["Import required"]);
      }
      
      IReadOnlyCollection<DbConnection> connections = 
        await _osmExporter.GetUnexportedOsmConnectionsAsync(tileId);
      OsmChangeset osmChangeset = _osmExporter.CreateChangeset(input.Comment);
      OsmChange osmChange = _osmExporter.GetOsmChange(connections);

      // send tile's changes to OSM
      OsmExportResult exportResult = await _osmExportBuilder
        .UseOsmApiUrl(_externalServices.OsmApiUrl)
        .UseOsmChange(osmChange)
        .UseOsmChangeset(osmChangeset)
        .UseUsername(input.Email)
        .UsePassword(input.Password)
        .UseClose()
        .ExportAsync();

      if (exportResult.ApiResponse.Status == OsmApiStatusCode.Unauthorized)
      {
        throw new BadHttpRequestException(_localizer["Invalid OSM Credentials"]);
      }

      if (exportResult.ChangesetId == null)
      {
        throw new BadHttpRequestException(_localizer["Changeset id is null"]);
      }

      // Get current user roles
      ApplicationUser user = await _userManager.GetUserAsync(User);

      // report export in database
      await _dbContext.OsmExportReports.AddAsync(new DbTileExportReport
      {
        TileId = tileId,
        CreatedAt = DateTime.Now.ToUniversalTime(),
        UserId = user.Id,
        TileReport = new(),
        ChangesetId = exportResult.ChangesetId.Value
      });

      foreach (DbConnection connection in connections)
      {
        connection.Exported = true;
      }

      try
      {
        await _dbContext.SaveChangesAsync();
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Problem with saving data to the database");
        throw new BadHttpRequestException(_localizer["Problem with saving data to the database"]);
      }

      return Ok();
    }

    [HttpGet("tiles/{tileId}/export/osc")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult> GetExportFile(Guid tileId)
    {
      IReadOnlyCollection<DbConnection> unexportedConnections = await _osmExporter.GetUnexportedOsmConnectionsAsync(tileId);
      OsmChange osmChange = _osmExporter.GetOsmChange(unexportedConnections);

      return File(osmChange.ToXml().ToBytes(), "text/xml", "osmchange.osc");
    }
  }
}