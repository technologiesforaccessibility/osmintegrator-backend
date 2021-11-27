using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;

using NCrontab;

namespace OsmIntegrator.Services
{
  public class OsmScheduler : IOsmScheduler
  {
    private readonly IConfiguration _configuration;
    private CancellationToken _cancellationToken;
    private readonly ILogger<OsmScheduler> _logger;

    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOsmUpdater _osmRefresherHelper;

    private readonly IOverpass _overpass;

    public OsmScheduler(
      ILogger<OsmScheduler> logger, 
      IServiceScopeFactory serviceScopeFactory, 
      HttpClient httpClient, 
      IConfiguration configuration, 
      IOsmUpdater osmRefresherHelper,
      IOverpass overpass)
    {
      _logger = logger;
      _configuration = configuration;
      _schedule = CrontabSchedule.Parse(_configuration["OsmCronInterval"], 
        new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
      _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
      _scopeFactory = serviceScopeFactory;
      _osmRefresherHelper = osmRefresherHelper;
      _overpass = overpass;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _cancellationToken = cancellationToken;

      Task.Run(async () =>
      {
        while (!_cancellationToken.IsCancellationRequested)
        {
          await Task.Delay(UntilNextExecution(), _cancellationToken);
          await Refresh();

          _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
        }
      }, _cancellationToken);

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
      return Task.CompletedTask;
    }

    private int UntilNextExecution() => 
      Math.Max(0, (int)_nextRun.Subtract(DateTime.Now).TotalMilliseconds);

    public async Task Refresh()
    {
      try
      {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
          ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

          Osm result = await _overpass.GetFullArea(dbContext, _cancellationToken);

          List<DbTile> tilesToRefresh = dbContext.Tiles
            .Include(x => x.Stops)
            .Include(x => x.TileUsers)
            .Where(x => x.TileUsers.Count() == 0)
            .ToList();

          await _osmRefresherHelper.Update(tilesToRefresh, dbContext, result);
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
      }
    }
  }
}