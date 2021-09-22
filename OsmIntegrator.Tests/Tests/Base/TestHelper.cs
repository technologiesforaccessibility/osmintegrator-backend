using osmintegrator;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace OsmIntegrator.Tests.Tests.Base
{
    class TestHelper
    {
        public static void RefillDatabase()
        {
            // Refill database
            IHost host = Program.CreateHostBuilder(null).Build();
            using (var scope = host.Services.CreateScope())
            {
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                DataInitializer _dataInitializer = host.Services.GetService<DataInitializer>();
                _dataInitializer.ClearDatabase(db);
                _dataInitializer.Initialize(db);
            }
        }
     }
}
