using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using osmintegrator.Database;
using osmintegrator.Models;

namespace osmintegrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StopController : ControllerBase
    {
        private readonly ILogger<StopController> _logger;
        private readonly IConfiguration _configuration;

        public StopController(
            ILogger<StopController> logger,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public IEnumerable<Stop> Get()
        {
            var applicationDbContext = new ApplicationDbContext(_configuration);
            return applicationDbContext.Stops.ToArray();
        }
    }
}
