using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels.Errors;

namespace OsmIntegrator.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TileController : ControllerBase
    {
        private readonly ILogger<StopController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public TileController(
            ILogger<StopController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _dbContext.Tiles.Where(x => x.GtfsStopsCount > 0).ToListAsync();
                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
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

                foreach (Stop stop in stops)
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown error while performing {nameof(Get)} method with parameter {id}.");
                return BadRequest(new UnknownError() { Title = ex.Message });
            }
        }
    }
}
