using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace ConsoleApp1
{
    public class DbSeeder : BackgroundService
    {
        private readonly ApplicationDbContext _dbContext;

        public DbSeeder(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _dbContext.GtfsStops.AddRangeAsync(
                new List<Stop>
                {
                new Stop { StopId=9006, TypeId = 1, StopName = "Katowice, Kolista 2", Lat=59.345f, Lon=18.4353f},
                new Stop { StopId=9007, TypeId = 1, StopName = "Katowice, Lipowa 3", Lat=59.645f, Lon=18.8353f},
                }
            );
            await _dbContext.SaveChangesAsync();
            Console.WriteLine("New records were added to Stops table.");
            Console.ReadLine();
        }
    }
}
