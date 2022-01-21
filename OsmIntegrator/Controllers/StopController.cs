using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Roles;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;
using System.Threading;
using OsmIntegrator.Database.Models;
using System.Linq;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Database.Models.Enums;

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
    private IOverpass _overpass;
    private IOsmUpdater _osmRefreshHelper;

    public StopController(
        ILogger<StopController> logger,
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        IMapper mapper,
        IStringLocalizer<StopController> localizer,
        IOverpass overpass,
        IOsmUpdater osmRefresherHelper
    )
    {
      _logger = logger;
      _dbContext = dbContext;
      _mapper = mapper;
      _localizer = localizer;
      _osmRefreshHelper = osmRefresherHelper;
      _overpass = overpass;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
    public async Task<ActionResult<List<Stop>>> Get()
    {
      var result = await _dbContext.Stops.ToListAsync();
      return Ok(_mapper.Map<List<Stop>>(result));
    }

    [HttpPut("ChangePosition")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<Stop>> ChangePosition(StopPositionData stop)
    {
      DbStop dbStop = await _dbContext.Stops.Include(
        x => x.Tile).FirstOrDefaultAsync(
          x => x.Id == stop.StopId);

      if (dbStop == null)
      {
        throw new BadHttpRequestException(_localizer["Stop cannot be find"]);
      }

      DbTile tile = dbStop.Tile;

      if (stop.Lat < tile.OverlapMinLat || stop.Lat > tile.OverlapMaxLat ||
        stop.Lon < tile.OverlapMinLon || stop.Lon > tile.OverlapMaxLon)
      {
        throw new BadHttpRequestException(_localizer["Stop is outside tile margin border"]);
      }

      if (dbStop.InitLat == null)
      {
        dbStop.InitLat = dbStop.Lat;
        dbStop.InitLon = dbStop.Lon;
      }

      dbStop.Lat = stop.Lat;
      dbStop.Lon = stop.Lon;

      Stop result = _mapper.Map<Stop>(dbStop);
      result.Tile = null;

      await _dbContext.SaveChangesAsync();
      return Ok(result);
    }

    [HttpPost("ResetPosition/{stopId}")]
    [Authorize(Roles = UserRoles.EDITOR + "," + UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR)]
    public async Task<ActionResult<Stop>> ResetPosition(string stopId)
    {
      DbStop dbStop = await _dbContext.Stops.FirstOrDefaultAsync(x => x.Id == Guid.Parse(stopId));

      if (dbStop == null)
      {
        throw new BadHttpRequestException(_localizer["Stop cannot be find"]);
      }

      if(dbStop.StopType != StopType.Gtfs)
      {
        throw new BadHttpRequestException(_localizer["Cannot move stop of type different than the GTFS"]);
      }

      if(dbStop.InitLat == null || dbStop.InitLon == null)
      {
        throw new BadHttpRequestException(_localizer["The stop already located on initial position"]);
      }

      dbStop.Lat = (double)dbStop.InitLat;
      dbStop.Lon = (double)dbStop.InitLon;
      dbStop.InitLat = null;
      dbStop.InitLon = null;
      Stop result = _mapper.Map<Stop>(dbStop);
      
      await _dbContext.SaveChangesAsync();
      return Ok(result);
    }

    [HttpPut("Update")]
    [Authorize(Roles = UserRoles.ADMIN)]
    public async Task<ActionResult> Update()
    {
      CancellationToken cancellationToken = new CancellationToken();
      Osm osm = await _overpass.GetFullArea(_dbContext, cancellationToken);

      List<DbTile> tilesToRefresh = _dbContext.Tiles.ToList();

      foreach (DbTile tile in tilesToRefresh)
      {
        await _osmRefreshHelper.Update(tile, _dbContext, osm);
      }

      return Ok();
    }
  }
}
