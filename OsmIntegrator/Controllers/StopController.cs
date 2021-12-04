using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
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

        [HttpPut("Update")]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<ActionResult> Update()
        {
          CancellationToken cancellationToken = new CancellationToken();
          Osm osm = await _overpass.GetFullArea(_dbContext, cancellationToken);

          List<DbTile> tilesToRefresh = _dbContext.Tiles
            .Include(x => x.Stops)
            .Include(x => x.TileUsers)
            .Where(x => x.TileUsers.Count() == 0)
            .ToList();

          foreach(DbTile tile in tilesToRefresh)
          {
            await _osmRefreshHelper.Update(tile, _dbContext, osm);
          }

          return Ok();
        }
    }
}
