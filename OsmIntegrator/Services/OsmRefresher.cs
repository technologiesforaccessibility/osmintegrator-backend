using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;
using Tag = OsmIntegrator.Database.Models.Tag;
using NCrontab;

namespace OsmIntegrator.Services
{
  public class OsmRefresher : IOsmRefresher
  {
    readonly IConfiguration _configuration;
    CancellationToken _cancelationToken;
    readonly ILogger<OsmRefresher> _logger;
    readonly HttpClient _httpClient;
    readonly CrontabSchedule _schedule;
    DateTime _nextRun;

    readonly IServiceScopeFactory _scopeFactory;

    public OsmRefresher(ILogger<OsmRefresher> logger, IServiceScopeFactory serviceScopeFactory, HttpClient httpClient, IConfiguration configuration)
    {
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
      _schedule = CrontabSchedule.Parse("0 * * * * *", new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
      _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
      _scopeFactory = serviceScopeFactory;
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
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
          ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
          List<DbTile> tilesToRefresh = dbContext.Tiles
            .Include(x => x.Stops)
            .Include(x => x.TileUsers)
            .Where(x => x.TileUsers.Count() == 0)
            .Where(x => x.Stops.Count() != 0)
            .ToList();
          HttpResponseMessage result = await _httpClient
            .SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://lz4.overpass-api.de/api/interpreter")
            {
              Content = new StringContent($"node [~'highway|railway'~'tram_stop|bus_stop'] (49.558915859179, 18.212585449219, 50.496783462923, 19.951171875); out meta;", Encoding.UTF8)
            },
            _cancelationToken
            );

          if (!result.IsSuccessStatusCode)
          {
            _logger.LogWarning(result.ReasonPhrase);
            return;
          }

          Stream responseStream = result.Content.ReadAsStream();

          XmlSerializer serializer = new XmlSerializer(typeof(Osm));

          Osm osmRoot = (Osm)serializer.Deserialize(responseStream);

          foreach (DbTile tile in tilesToRefresh)
          {
            foreach (Node node in osmRoot.Node)
            {
              if (tile.MinLat > double.Parse(node.Lat, CultureInfo.InvariantCulture)
                || tile.MaxLat < double.Parse(node.Lat, CultureInfo.InvariantCulture)
                || tile.MinLon > double.Parse(node.Lon, CultureInfo.InvariantCulture)
                || tile.MaxLon < double.Parse(node.Lon, CultureInfo.InvariantCulture))
              {
                // node is outside boundary of current tile
                continue;
              }
              DbStop existingStop = tile.Stops.FirstOrDefault(x => x.StopId == long.Parse(node.Id));

              if (existingStop != null)
              {
                if (existingStop.Changeset == node.Changeset && existingStop.Version == node.Version)
                {
                  continue;
                }

                existingStop.Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture);
                existingStop.Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture);
                existingStop.Version = node.Version;
                existingStop.Changeset = node.Changeset;

                PopulateTags(existingStop, node);
                dbContext.Stops.Update(existingStop);
              }
              else
              {
                DbStop stop = new DbStop
                {
                  Id = Guid.NewGuid(),
                  StopId = long.Parse(node.Id),
                  Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
                  Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
                  StopType = StopType.Osm,
                  ProviderType = ProviderType.Ztm,
                  Version = node.Version,
                  Changeset = node.Changeset
                };

                PopulateTags(stop, node);

                tile.Stops.Add(stop);
              }
            }
            await dbContext.SaveChangesAsync();

            foreach (DbStop stop in tile.Stops)
            {
              if (!osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.StopId))
              {
                stop.IsDeleted = true;
                dbContext.Stops.Update(stop);
              }
            }
            await dbContext.SaveChangesAsync();
          }
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
      }
    }

    void PopulateTags(DbStop stop, Node node)
    {
      List<Tag> tempTags = new List<Tag>();

      node.Tag.ForEach(x => tempTags.Add(new Tag()
      {
        Key = x.K,
        Value = x.V
      }));
      stop.Tags = tempTags;

      var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "name");
      stop.Name = nameTag?.Value;

      var refTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "ref");
      long refVal;
      if (refTag != null && long.TryParse(refTag.Value, out refVal))
      {
        stop.Ref = refVal;
      }

      var localRefTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "local_ref");
      if (localRefTag != null && !string.IsNullOrEmpty(localRefTag.Value))
      {
        stop.Number = localRefTag.Value;
      }
    }

  }
}