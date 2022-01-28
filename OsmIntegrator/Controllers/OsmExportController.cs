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
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Tools;
using OsmIntegrator.OsmApi;
using OsmIntegrator.Extensions;
using System.Linq;
using OsmIntegrator.Validators;

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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOsmExportBuilder _osmExportBuilder;
    private readonly IExternalServicesConfiguration _externalServices;
    private readonly ITileExportValidator _tileExportValidator;

    public OsmExportController(
        ApplicationDbContext dbContext,
        ILogger<OsmExportController> logger,
        IMapper mapper,
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
      _mapper = mapper;
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
      if (!await _tileExportValidator.ValidateDelayAsync(tileId))
      {
        return BadRequest();
      }

      // send tile's changes to OSM
      uint changesetId = await _osmExportBuilder
        .UseTile(tileId)
        .UseChangesetComment(input.Comment)
        .UseOsmApiUrl(_externalServices.OsmApiUrl)
        .UseUsername(input.Email)
        .UsePassword(input.Password)
        .UseClose()
        .ExportAsync();

      // Get current user roles
      ApplicationUser user = await _userManager.GetUserAsync(User);

      // report export in database
      await _dbContext.ExportReports.AddAsync(new DbTileExportReport
      {
        TileId = tileId,
        CreatedAt = DateTime.Now,
        UserId = user.Id,
        TileReport = new(),
        ChangesetId = changesetId
      });

      await _dbContext.SaveChangesAsync();

      return Ok();
    }

    [HttpGet("tiles/{tileId}/export/osc")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult> GetExportFile(Guid tileId)
    {
      OsmChange osmChange = await _osmExporter.GetOsmChangeAsync(tileId, 0);

      return File(osmChange.ToXml().ToBytes(), "text/xml", "osmchange.osc");
    }
  }
}