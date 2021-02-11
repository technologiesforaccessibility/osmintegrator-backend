using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;

namespace OsmIntegrator.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StopController : ControllerBase
    {
        private readonly ILogger<StopController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public StopController(
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
                var result = await _dbContext.Stops.ToListAsync();
                return Ok(result);

            } catch(Exception ex)
            {
                _logger.LogWarning(ex, "Unknown error while performing ");
                return BadRequest(new UnknownError() { Description = ex.Message });
            }
        }
    }
}
