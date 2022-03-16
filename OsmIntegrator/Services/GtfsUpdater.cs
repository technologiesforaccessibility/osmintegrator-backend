using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;
using Tag = OsmIntegrator.Database.Models.JsonFields.Tag;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Database.Models.Enums;
using System;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.Database.Models.CsvObjects;

namespace OsmIntegrator.Services
{
  public class GtfsUpdater : IGtfsUpdater
  {
    private readonly IGtfsReportsFactory _reportsFactory;
    private readonly ILogger<IOsmUpdater> _logger;

    public GtfsUpdater(IGtfsReportsFactory reportsFactory, ILogger<IOsmUpdater> logger)
    {
      _reportsFactory = reportsFactory;
      _logger = logger;
    }

    private void RemoveConnections(ApplicationDbContext dbContext)
    {
      List<DbConnection> connectionsToDelete = dbContext.Connections
        .Include(x => x.OsmStop)
        .Include(x => x.GtfsStop)
        .Where(x => x.OsmStop.IsDeleted == true)
        .ToList();

      dbContext.Connections.RemoveRange(connectionsToDelete);
    }

    private bool isChanged(GtfsStop stop, DbStop dbStop)
    {
      if (double.Parse(stop.stop_lat, CultureInfo.InvariantCulture) != dbStop.Lat ||
          double.Parse(stop.stop_lat, CultureInfo.InvariantCulture) != dbStop.Lon ||
          stop.stop_name != dbStop.Name ||
          stop.stop_code != dbStop.Number)
      {
        return true;
      }

      return false;
    }

    /// <summary>
    /// Check if there were any changes made in GTFS stops
    /// </summary>
    /// <param name="stops">Array of gtfs stops from uploaded csv</param>
    /// <param name="osmRoot">OSM file as an object structure</param>
    /// <returns>True if there were changes in stops array</returns>
    public bool ContainsChanges(GtfsStop[] stops, DbTile[] tiles, Osm osmRoot)
    {
      foreach (Node node in osmRoot.Node)
      {
        var gtfsStop = stops.FirstOrDefault(x => x.stop_id == long.Parse(node.Id));
        var existingStop = tiles.First(tile => tile.Stops.Any(x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm)).Stops.FirstOrDefault(
          x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm);

        if (gtfsStop != null)
        {
          if (!isChanged(gtfsStop, existingStop))
          {
            continue;
          }

          // Stop modified
          return true;
        }
        // Stop needs to be added
        return true;
      }

      foreach (var stop in stops)
      {
        if (!osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.stop_id))
        {
          return true;
        }
      }

      // Nothing has changed
      return false;
    }

    public async Task<GtfsImportReport> Update(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
      try
      {
        GtfsImportReport report = await ProcessStops(stops, tiles, dbContext, osmRoot);
        await dbContext.SaveChangesAsync();
        RemoveConnections(dbContext);

        // if (report.Stops.Count > 0)
        // {
        //       dbContext.ChangeReports.Add(new DbTileImportReport
        //       {
        //         CreatedAt = DateTime.Now.ToUniversalTime(),
        //         TileId = x.TileId, // Tile id was saved during the report creation
        //         TileReport = x
        //       });
        // }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return report;
      }
      catch (Exception e)
      {
        await transaction.RollbackAsync();
        _logger.LogError(e, $"Problem with updating gtfs stops");
        throw;
      }
    }

    private async Task<GtfsImportReport> ProcessStops(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      GtfsImportReport report = _reportsFactory.Create();

      foreach (GtfsStop stop in stops)
      {
        Node node = osmRoot.Node.FirstOrDefault(n => double.Parse(n.Id) == stop.stop_id);
        if (node != null)
        {
          DbTile stopTile = tiles.FirstOrDefault(tile => tile.Stops.Any(s => s.StopId == long.Parse(node.Id)));
          if (stopTile == null) continue;
          DbStop existingStop = stopTile.Stops.FirstOrDefault(s => s.StopId == long.Parse(node.Id));

          if (stop != null && existingStop != null)
          {
            bool deletionReverted = false;
            if (existingStop.IsDeleted)
            {
              deletionReverted = true;
              existingStop.IsDeleted = false;
            }

            if (isChanged(stop, existingStop))
            {
              ModifyStop(stop, existingStop, dbContext, report, deletionReverted);
            }
          }
        }
        else
        {
          if (tiles.Any(tile => tile.Stops.Any(s => s.StopId == stop.stop_id)))
          {
            DbTile tile = tiles.FirstOrDefault(tile => tile.Stops.Any(s => s.StopId == stop.stop_id));
            DbStop existingStop = tile.Stops.FirstOrDefault(s => s.StopId == stop.stop_id);
            ModifyStop(stop, existingStop, dbContext, report, false);
            continue;
          }

          double stopLat = double.Parse(stop.stop_lat, CultureInfo.InvariantCulture);
          double stopLon = double.Parse(stop.stop_lon, CultureInfo.InvariantCulture);

          DbTile stopTile = tiles.FirstOrDefault(tile =>
            stopLat >= tile.MinLat &&
            stopLat <= tile.MaxLat &&
            stopLon >= tile.MinLon &&
            stopLon <= tile.MaxLon
          );

          if (stopTile == null) continue;

          AddStop(node, stopTile, report, stop);
        }
      }

      foreach (GtfsStop stop in stops)
      {
        if (!osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.stop_id))
        {
          await RemoveStop(stop, dbContext, report);
        }
      }

      return report;
    }
    private void ModifyStop(GtfsStop existingStop, DbStop dbStop, ApplicationDbContext dbContext, GtfsImportReport report, bool deletionReverted)
    {
      // Report - new stop
      ReportStop reportStop =
        _reportsFactory.CreateStop(report, existingStop, ChangeAction.Modified, deletionReverted);

      dbStop.Version = 0;

      if (double.Parse(existingStop.stop_lat, CultureInfo.InvariantCulture) != dbStop.Lat)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Lat), existingStop.stop_lat, dbStop.Lat.ToString(), ChangeAction.Modified);

        dbStop.Lat = double.Parse(existingStop.stop_lat, CultureInfo.InvariantCulture);
      }

      if (double.Parse(existingStop.stop_lon, CultureInfo.InvariantCulture) != dbStop.Lon)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Lon), existingStop.stop_lon, dbStop.Lon.ToString(), ChangeAction.Modified);

        dbStop.Lon = double.Parse(existingStop.stop_lon, CultureInfo.InvariantCulture);
      }

      if (existingStop.stop_name != dbStop.Name)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Name), existingStop.stop_name, dbStop.Name, ChangeAction.Modified);

        dbStop.Name = existingStop.stop_name;
      }

      if (existingStop.stop_code != dbStop.Number)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Number), existingStop.stop_code, dbStop.Number, ChangeAction.Modified);

        dbStop.Number = existingStop.stop_code;
      }

      dbContext.Stops.Update(dbStop);
    }

    private void AddStop(Node node, DbTile tile, GtfsImportReport report, GtfsStop gtfsStop)
    {
      DbStop stop = new DbStop
      {
        StopId = gtfsStop.stop_id,
        Lat = double.Parse(gtfsStop.stop_lat, CultureInfo.InvariantCulture),
        Lon = double.Parse(gtfsStop.stop_lon, CultureInfo.InvariantCulture),
        StopType = StopType.Gtfs,
        ProviderType = ProviderType.Ztm,
        Version = 0,
        TileId = tile.Id,
        Tile = tile,
      };

      ReportStop reportStop = _reportsFactory.CreateStop(report, gtfsStop, ChangeAction.Added);
      tile.Stops.Add(stop);
    }

    private async Task RemoveStop(GtfsStop stop, ApplicationDbContext dbContext, GtfsImportReport report)
    {
      DbStop dbStop = await dbContext.Stops.FirstOrDefaultAsync(x => x.StopId == stop.stop_id);
      if (dbStop == null || dbStop.IsDeleted) return;
      _reportsFactory.CreateStop(report, stop, ChangeAction.Removed);
      dbStop.IsDeleted = true;
      dbContext.Stops.Update(dbStop);
    }
  }
}