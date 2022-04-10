using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Roles;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Validators;
using CsvHelper;
using System.IO;
using System.Globalization;
using OsmIntegrator.Database.Models.CsvObjects;

namespace OsmIntegrator.Controllers;

[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ApiController]
[Route("api/[controller]/[action]")]
[EnableCors("AllowOrigin")]
public class GtfsController : ControllerBase
{
  private readonly ILogger<TileController> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IMapper _mapper;
  private readonly IStringLocalizer<TileController> _localizer;
  private readonly IOverpass _overpass;
  private readonly IOsmUpdater _osmUpdater;
  private readonly ITileExportValidator _tileExportValidator;
  private readonly IOsmExporter _osmExporter;
  private readonly IGtfsUpdater _gtfsUpdater;

  public GtfsController(
    ILogger<TileController> logger,
    ApplicationDbContext dbContext,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<TileController> localizer,
    IOsmUpdater refresherHelper,
    IOverpass overpass,
    ITileExportValidator tileExportValidator,
    IOsmExporter osmExporter,
    IGtfsUpdater gtfsUpdater
    )
  {
    _logger = logger;
    _dbContext = dbContext;
    _mapper = mapper;
    _userManager = userManager;
    _localizer = localizer;
    _osmUpdater = refresherHelper;
    _overpass = overpass;
    _tileExportValidator = tileExportValidator;
    _osmExporter = osmExporter;
    _gtfsUpdater = gtfsUpdater;
  }

  [HttpPut()]
  [Authorize(Roles =
    UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
  public async Task<ActionResult<Report>> UpdateStops(IFormFile file)
  {
    if (file == null || (file.ContentType != "text/plain" && file.ContentType != "text/csv"))
    {
      throw new BadHttpRequestException(_localizer["Uploaded file is not in CSV format"]);
    }

    using (var reader = new StreamReader(file.OpenReadStream()))
    {
      try
      {
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          var recordsArray = csv.GetRecords<GtfsStop>().ToArray();
          if (recordsArray.Length == 0)
          {
            throw new BadHttpRequestException(_localizer["CSV file is empty"]);
          }

          var report = await _gtfsUpdater.Update(recordsArray, await GetAllTilesAsync(), _dbContext);
          return Ok(new Report { Value = report.GetResultText(_localizer) });
        }
      }
      catch (CsvHelper.CsvHelperException)
      {
        throw new BadHttpRequestException(_localizer["Problem with parsing CSV file"]);
      }
    }
  }

  private async Task<DbTile[]> GetAllTilesAsync()
  {
    var tiles = await _dbContext.Tiles
      .Include(tile => tile.Stops).ToArrayAsync();

    return tiles;
  }
}