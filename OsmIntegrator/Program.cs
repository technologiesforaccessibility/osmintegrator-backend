using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Services;
using OsmIntegrator.Interfaces;
using System.Reflection;
using OsmIntegrator.Tools;

namespace osmintegrator
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
      try
      {
        logger.Debug($"Starting application. Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
        IHost host = CreateHostBuilder(args).Build();
        InitializeData(host);
        host.Run();
      }
      catch (Exception exception)
      {
        logger.Error(exception, "Stopped program because of exception");
        throw;
      }
      finally
      {
        NLog.LogManager.Shutdown();
      }
    }

    public static void InitializeData(IHost host)
    {
      using (var scope = host.Services.CreateScope())
      {
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        DataInitializer _dataInitializer = host.Services.GetService<DataInitializer>();

        _dataInitializer.Initialize(db);
      }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
              config.AddEnvironmentVariables(prefix: "OsmIntegrator_");
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
            })
            .ConfigureServices(services =>
            {
              services.AddHttpClient();
              services.AddSingleton<IReportsFactory, ReportsFactory>();
              services.AddSingleton<IGtfsReportsFactory, GtfsReportsFactory>();
              services.AddSingleton<IOverpass, Overpass>();
              services.AddSingleton<IOsmUpdater, OsmUpdater>();
              services.AddSingleton<IGtfsUpdater, GtfsUpdater>();
              services.AddHostedService<OsmScheduler>();
            })
            .ConfigureLogging(logging =>
            {
              logging.ClearProviders();
              logging.SetMinimumLevel(LogLevel.Trace);
            }).UseNLog();
  }
}
