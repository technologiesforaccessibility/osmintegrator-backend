using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels.Errors;
using System.Collections.Generic;
using AutoMapper;

namespace OsmIntegrator.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TileController : ControllerBase
    {
        private readonly ILogger<StopController> _logger;
        private readonly ApplicationDbContext _dbContext;

        private readonly IMapper _mapper;

        public TileController(
            ILogger<StopController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<Tile>>> Get()
        {
            try
            {
                List<DbTile> result = await _dbContext.Tiles.Where(
                        x => x.GtfsStopsCount > 0).ToListAsync();
                return Ok(_mapper.Map<List<Tile>>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tile>> Get(string id)
        {
            try
            {
                Guid guidId = Guid.Parse(id);
                var result = await _dbContext.Tiles.SingleOrDefaultAsync(x => x.Id == guidId);

                if (result == null)
                {
                    Error error = new Error();
                    error.Title = $"Unable to find tile with id {id}.";
                    error.Message = "General error";
                    return BadRequest(error);
                }

                var stops = await _dbContext.Stops.Where(x =>
                    x.Lon > result.OverlapMinLon && x.Lon <= result.OverlapMaxLon &&
                    x.Lat > result.OverlapMinLat && x.Lat <= result.OverlapMaxLat).ToListAsync();

                foreach (DbStop stop in stops)
                {
                    if (stop.Lon > result.MinLon && stop.Lon <= result.MaxLon &&
                        stop.Lat > result.MinLat && stop.Lat <= result.MaxLat)
                    {
                        stop.OutsideSelectedTile = false;
                        continue;
                    }
                    stop.OutsideSelectedTile = true;
                }

                stops.ForEach(x => x.Tile = null);

                result.Stops = stops;

                return Ok(_mapper.Map<Tile>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(Get)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }
    }
}
