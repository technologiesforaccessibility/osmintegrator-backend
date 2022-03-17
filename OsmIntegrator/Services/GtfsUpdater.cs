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
          double.Parse(stop.stop_lon, CultureInfo.InvariantCulture) != dbStop.Lon ||
          stop.stop_name != dbStop.Name ||
          stop.stop_code != dbStop.Number)
      {
        return true;
      }

      return false;
    }

    public async Task<GtfsImportReport> Update(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext)
    {
      await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
      try
      {
        GtfsImportReport report = await ProcessStops(stops, tiles, dbContext);
        await dbContext.SaveChangesAsync();
        RemoveConnections(dbContext);

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

    private async Task<GtfsImportReport> ProcessStops(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext)
    {
      GtfsImportReport report = _reportsFactory.Create();

      foreach (GtfsStop stop in stops)
      {
        if (tiles.Any(tile => tile.Stops.Any(s => s.StopId == stop.stop_id)))
        {
          DbTile tile = tiles.FirstOrDefault(tile => tile.Stops.Any(s => s.StopId == stop.stop_id));
          if (tile == null) continue;
          DbStop existingStop = tile.Stops.FirstOrDefault(s => s.StopId == stop.stop_id);

          if (stop != null && existingStop != null)
          {

            bool deletionReverted = false;
            if (existingStop.IsDeleted)
            {
              deletionReverted = true;
              existingStop.IsDeleted = false;
            }

            if (isChanged(stop, existingStop) || deletionReverted)
            {
              ModifyStop(stop, existingStop, dbContext, report, deletionReverted);
            }
          }
        }
        else
        {

          double stopLat = double.Parse(stop.stop_lat, CultureInfo.InvariantCulture);
          double stopLon = double.Parse(stop.stop_lon, CultureInfo.InvariantCulture);

          DbTile stopTile = tiles.FirstOrDefault(tile =>
            stopLat >= tile.MinLat &&
            stopLat <= tile.MaxLat &&
            stopLon >= tile.MinLon &&
            stopLon <= tile.MaxLon
          );

          if (stopTile == null) continue;

          AddStop(stopTile, report, stop);
        }
      }

      foreach (DbTile tile in tiles)
      {
        foreach (DbStop dbStop in tile.Stops)
        {
          GtfsStop stopFromFile = stops.FirstOrDefault(s => s.stop_id == dbStop.StopId);
          if (stopFromFile == null && dbStop.StopType == StopType.Gtfs)
          {
            await RemoveStop(dbStop, dbContext, report);
          }
        }
      }

      return report;
    }
    private void ModifyStop(GtfsStop existingStop, DbStop dbStop, ApplicationDbContext dbContext, GtfsImportReport report, bool deletionReverted)
    {
      // Report - new stop
      ReportStop reportStop =
        _reportsFactory.CreateStop(report, dbStop, ChangeAction.Modified, deletionReverted);

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

    private void AddStop(DbTile tile, GtfsImportReport report, GtfsStop gtfsStop)
    {
      DbStop stop = new DbStop
      {
        Name = gtfsStop.stop_name,
        Number = gtfsStop.stop_code,
        StopId = gtfsStop.stop_id,
        Lat = double.Parse(gtfsStop.stop_lat, CultureInfo.InvariantCulture),
        Lon = double.Parse(gtfsStop.stop_lon, CultureInfo.InvariantCulture),
        StopType = StopType.Gtfs,
        ProviderType = ProviderType.Ztm,
        Version = 0,
        TileId = tile.Id,
        Tile = tile,
      };

      ReportStop reportStop = _reportsFactory.CreateStop(report, stop, ChangeAction.Added);
      tile.Stops.Add(stop);
    }

    private async Task RemoveStop(DbStop stop, ApplicationDbContext dbContext, GtfsImportReport report)
    {
      DbStop dbStop = await dbContext.Stops
        .Include(s => s.OsmConnections)
        .Include(s => s.GtfsConnections)
        .FirstOrDefaultAsync(x => x.StopId == stop.StopId);

      if (dbStop == null || dbStop.IsDeleted) return;
      _reportsFactory.CreateStop(report, stop, ChangeAction.Removed);
      dbStop.IsDeleted = true;

      dbContext.Connections.RemoveRange(dbStop.OsmConnections);
      dbContext.Connections.RemoveRange(dbStop.GtfsConnections);
      dbContext.Stops.Update(dbStop);
    }
  }
}