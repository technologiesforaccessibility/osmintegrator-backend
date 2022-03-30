using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore;

using NCrontab;

namespace OsmIntegrator.Services
{
  public class EmailScheduler : IEmailScheduler
  {
    private readonly IConfiguration _configuration;

    private CancellationToken _cancellationToken;

    private readonly ILogger<EmailScheduler> _logger;

    private readonly CrontabSchedule _schedule;

    private DateTime _nextRun;

    private readonly IServiceScopeFactory _scopeFactory;

    public EmailScheduler(
      ILogger<EmailScheduler> logger,
      IServiceScopeFactory serviceScopeFactory,
      HttpClient httpClient,
      IConfiguration configuration)
    {
      _logger = logger;
      _configuration = configuration;
      _schedule = CrontabSchedule.Parse(_configuration["OsmCronInterval"],
        new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
      _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
      _scopeFactory = serviceScopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _cancellationToken = cancellationToken;

      Task.Run(async () =>
      {
        while (!_cancellationToken.IsCancellationRequested)
        {
          await Task.Delay(UntilNextExecution(), _cancellationToken);
          SendEmail();

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

    public void SendEmail()
    {
      try
      {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
          ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

          List<DbTile> tiles = dbContext.Tiles
            .Include(t => t.Stops)
              .ThenInclude(s => s.OsmConnections)
            .Include(t => t.Stops)
              .ThenInclude(s => s.GtfsConnections)
            .ToList();
          List<ApplicationUser> users = dbContext.Users.ToList();

          if (tiles == null || tiles.Count() == 0) return;

          IEmailHelper emailHelper = scope.ServiceProvider.GetRequiredService<IEmailHelper>();

          users.ForEach(user =>
          {
            tiles.ForEach(async tile =>
            {
              if (tile.Stops != null && tile.IsAccessibleBy(user.Id))
              {
                List<DbConnection> userConnections = dbContext.Connections
                  .Where(c => c.UserId == user.Id)
                  .OrderBy(c => c.CreatedAt)
                  .ToList();


                if (userConnections != null && userConnections.Count() > 0)
                {
                  DbConnection firstConnection = userConnections[0];

                  TimeSpan timeSinceCreation = DateTime.Now - firstConnection.CreatedAt;
                  TimeSpan maxOccupationPeriod = new TimeSpan(int.Parse(_configuration["OsmTileMaxOccupationPeriodInDays"]), 0, 0, 0);

                  if (timeSinceCreation >= maxOccupationPeriod)
                  {
                    await emailHelper.SendTileOccupiedMessageAsync(user, _configuration["OsmIntegratorManualUrl"], tile.X, tile.Y);
                  }
                }
              }
            });
          });
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
      }
    }
  }
}