using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.ApiModels.OsmExport;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Extensions;
using OsmIntegrator.Interfaces;
using OsmIntegrator.OsmApi;
using OsmIntegrator.Roles;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Controllers;

[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ApiController]
[Route("api/[controller]/[action]")]
[EnableCors("AllowOrigin")]
public class UpdateRefsCommandController : ControllerBase
{
  private readonly ILogger<UpdateRefsCommandController> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly IOsmExporter _osmExporter;
  private readonly IOsmExportBuilder _osmExportBuilder;
  private readonly IExternalServicesConfiguration _externalServices;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IStringLocalizer<UpdateRefsCommandController> _localizer;

  public UpdateRefsCommandController(
    ILogger<UpdateRefsCommandController> logger,
    ApplicationDbContext dbContext,
    IOsmExporter osmExporter,
    IOsmExportBuilder osmExportBuilder,
    IExternalServicesConfiguration externalServices,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<UpdateRefsCommandController> localizer)
  {
    _logger = logger;
    _dbContext = dbContext;
    _osmExporter = osmExporter;
    _osmExportBuilder = osmExportBuilder;
    _externalServices = externalServices;
    _userManager = userManager;
    _localizer = localizer;
  }

  private OsmChange GetOsmChange()
  {
    IReadOnlyCollection<DbConnection> connections = _dbContext
      .Connections
      .Include(x => x.OsmStop)
      .Include(x => x.GtfsStop)
      .OnlyExported().ToList();
    return _osmExporter.GetOsmChange(connections);
  }
  
  private OsmChangeset GetOsmChangeset()
  {
    StringBuilder sb = new();
    sb.Append("Updating ref tags for all stops connected by osmintegrator.eu application");
    sb.Append("ref was updated with the stop code the same as local_ref.");
    sb.Append(
      "New tag was added with name ref:metropolia. Tag contains stop unique identifier from ZTM public transport provider db.");
    sb.Append(Constants.IMPORT_WIKI_ADDRESS);

    return _osmExporter.CreateChangeset(sb.ToString());
  }
  
  [HttpGet]
  [Authorize(Roles = UserRoles.SUPERVISOR)]
  public ActionResult GetOsmChangeFile()
  {
    OsmChange osmChange = GetOsmChange();
    return File(osmChange.ToXml().ToBytes(), "text/xml", "osmchange.osc");
  }

  [HttpPost]
  [Authorize(Roles = UserRoles.SUPERVISOR)]
  public async Task<ActionResult<string>> UpdateRefs([FromBody] LoginData credentials)
  {
    // send tile's changes to OSM
    OsmExportResult exportResult = await _osmExportBuilder
      .UseOsmApiUrl(_externalServices.OsmApiUrl)
      .UseOsmChange(GetOsmChange())
      .UseOsmChangeset(GetOsmChangeset())
      .UseUsername(credentials.Email)
      .UsePassword(credentials.Password)
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
      CreatedAt = DateTime.Now.ToUniversalTime(),
      UserId = user.Id,
      ChangesetId = exportResult.ChangesetId.Value
    });

    return Ok();
  }
}