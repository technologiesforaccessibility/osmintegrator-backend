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

    private bool isChanged(GtfsStop stop, DbStop dbStop, Node node)
    {
      if (stop.stop_lat != node.Lat ||
          stop.stop_lon != node.Lon ||
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
          if (!isChanged(gtfsStop, existingStop, node))
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

      foreach (Node node in osmRoot.Node)
      {
        if (node == null) continue;
        GtfsStop gtfsStop = stops.FirstOrDefault(stop => stop.stop_id == long.Parse(node.Id));
        DbTile stopTile = tiles.FirstOrDefault(tile => tile.Stops.Any(stop => stop.StopId == long.Parse(node.Id)));
        if (stopTile == null) continue;
        DbStop existingStop = stopTile.Stops.FirstOrDefault(stop => stop.StopId == long.Parse(node.Id));

        if (gtfsStop != null && existingStop != null)
        {
          bool deletionReverted = false;
          if (existingStop.IsDeleted)
          {
            deletionReverted = true;
            existingStop.IsDeleted = false;
          }

          if (existingStop.Changeset == node.Changeset && existingStop.Version == node.Version)
          {
            if (deletionReverted)
            {
              _reportsFactory.CreateStop(
                report, node, gtfsStop, ChangeAction.Modified, deletionReverted);
            }
            continue;
          }

          ModifyStop(gtfsStop, existingStop, node, dbContext, report, deletionReverted);
        }
        else if (gtfsStop != null && existingStop == null)
        {
          AddStop(node, stopTile, report, gtfsStop);
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
    private void ModifyStop(GtfsStop existingStop, DbStop dbStop, Node node, ApplicationDbContext dbContext, GtfsImportReport report, bool deletionReverted)
    {
      // Report - new stop
      ReportStop reportStop =
        _reportsFactory.CreateStop(report, node, existingStop, ChangeAction.Modified, deletionReverted);

      dbStop.Version = node.Version;
      dbStop.Changeset = node.Changeset;

      double nodeLat = double.Parse(node.Lat, CultureInfo.InvariantCulture);
      if (double.Parse(existingStop.stop_lat) != nodeLat)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.stop_lat), nodeLat.ToString(), existingStop.stop_lat, ChangeAction.Modified);

        dbStop.Lat = nodeLat;
      }

      double nodeLong = double.Parse(node.Lon, CultureInfo.InvariantCulture);
      if (double.Parse(existingStop.stop_lon) != nodeLong)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.stop_lon), nodeLong.ToString(), existingStop.stop_lon, ChangeAction.Modified);

        dbStop.Lon = nodeLong;
      }

      dbContext.Stops.Update(dbStop);
    }

    private void AddStop(Node node, DbTile tile, GtfsImportReport report, GtfsStop gtfsStop)
    {
      DbStop stop = new DbStop
      {
        StopId = long.Parse(node.Id),
        Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
        Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
        StopType = StopType.Gtfs,
        ProviderType = ProviderType.Ztm,
        Version = node.Version,
        Changeset = node.Changeset,
        TileId = tile.Id,
        Tile = tile,
        Tags = new List<Tag>()
      };

      ReportStop reportStop = _reportsFactory.CreateStop(report, node, gtfsStop, ChangeAction.Added);
      tile.Stops.Add(stop);
    }

    private async Task RemoveStop(GtfsStop stop, ApplicationDbContext dbContext, GtfsImportReport report)
    {
      DbStop dbStop = await dbContext.Stops.FirstOrDefaultAsync(x => x.StopId == stop.stop_id);
      if (dbStop == null || dbStop.IsDeleted) return;
      _reportsFactory.CreateStop(report, null, stop, ChangeAction.Removed);
      dbStop.IsDeleted = true;
      dbContext.Stops.Update(dbStop);
    }
  }
}