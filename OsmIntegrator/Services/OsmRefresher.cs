using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
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
  public class OsmRefresher : IOsmRefresher
  {
    readonly IConfiguration _configuration;
    CancellationToken _cancelationToken;
    readonly ILogger<OsmRefresher> _logger;

    readonly CrontabSchedule _schedule;
    DateTime _nextRun;

    readonly IServiceScopeFactory _scopeFactory;
    readonly IOsmRefresherHelper _osmRefresherHelper;

    public OsmRefresher(ILogger<OsmRefresher> logger, IServiceScopeFactory serviceScopeFactory, HttpClient httpClient, IConfiguration configuration, IOsmRefresherHelper osmRefresherHelper)
    {
      _logger = logger;
      _configuration = configuration;
      _schedule = CrontabSchedule.Parse("0 0 2 * * *", new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
      _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
      _scopeFactory = serviceScopeFactory;
      _osmRefresherHelper = osmRefresherHelper;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _cancelationToken = cancellationToken;

      Task.Run(async () =>
      {
        while (!_cancelationToken.IsCancellationRequested)
        {
          await Task.Delay(UntilNextExecution(), _cancelationToken);
          await Refresh();

          _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
        }
      }, _cancelationToken);

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
      return Task.CompletedTask;
    }

    int UntilNextExecution() => Math.Max(0, (int)_nextRun.Subtract(DateTime.Now).TotalMilliseconds);

    async Task Refresh()
    {
      try
      {
        Osm result = await _osmRefresherHelper.GetContent(
          new StringContent(
            $"node [~'highway|railway'~'tram_stop|bus_stop'] (49.558915859179, 18.212585449219, 50.496783462923, 19.951171875); out meta;",
            Encoding.UTF8),
          _cancelationToken
        );
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
          ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
          List<DbTile> tilesToRefresh = dbContext.Tiles
            .Include(x => x.Stops)
            .Include(x => x.TileUsers)
            .Where(x => x.TileUsers.Count() == 0)
            .Where(x => x.Stops.Count() != 0)
            .ToList();


          await _osmRefresherHelper.Refresh(tilesToRefresh, dbContext, result);
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
      }
    }

  }
}