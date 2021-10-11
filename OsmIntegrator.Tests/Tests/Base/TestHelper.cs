using osmintegrator;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Helpers;
using System.Linq;

namespace OsmIntegrator.Tests.Tests.Base
{
    class TestHelper
    {
        public static void RefillDatabase()
        {
            var stopDict = StopHelper.GetTestStopIdDict();

            // Refill database
            IHost host = Program.CreateHostBuilder(null).Build();
            using (var scope = host.Services.CreateScope())
            {
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                DataInitializer _dataInitializer = host.Services.GetService<DataInitializer>();
                _dataInitializer.ClearDatabase(db);

                List<DbStop> gtfsStops = _dataInitializer.GetGtfsStopsList().Where(f => stopDict.Where(s => s.Value==1).Any(s => s.Key==f.StopId)).ToList();
                List<DbStop> osmStops = _dataInitializer.GetOsmStopsList().Where(f => stopDict.Where(s => s.Value==2).Any(s => s.Key==f.StopId)).ToList();
                _dataInitializer.Initialize(db, gtfsStops, osmStops);
            }
        }
     }
}
