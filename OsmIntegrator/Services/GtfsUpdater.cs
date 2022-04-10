using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
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
        .Include(x => x.GtfsStop)
        .Where(x => x.GtfsStop.IsDeleted == true)
        .ToList();

      dbContext.Connections.RemoveRange(connectionsToDelete);
    }

    private bool IsChanged(GtfsStop stop, DbStop dbStop)
    {
      return double.Parse(stop.StopLat, CultureInfo.InvariantCulture) != dbStop.Lat ||
          double.Parse(stop.StopLon, CultureInfo.InvariantCulture) != dbStop.Lon ||
          stop.StopName != dbStop.Name ||
          stop.StopCode != dbStop.Number;
    }

    public async Task<GtfsImportReport> Update(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext)
    {
      await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
      try
      {
        GtfsImportReport report = await ProcessStops(stops, tiles, dbContext);
        await dbContext.SaveChangesAsync();
        RemoveConnections(dbContext);

        if (report.Stops.Count > 0)
        {
          dbContext.GtfsImportReports.Add(new DbGtfsImportReport
          {
            CreatedAt = DateTime.Now.ToUniversalTime(),
            GtfsReport = report
          });
        }

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
        if (tiles.Any(tile => tile.Stops.Any(s => s.StopId == stop.StopId)))
        {
          DbTile tile = tiles.FirstOrDefault(tile => tile.Stops.Any(s => s.StopId == stop.StopId));
          if (tile == null) continue;
          DbStop dbStop = tile.Stops.FirstOrDefault(s => s.StopId == stop.StopId);

          if (stop != null && dbStop != null)
          {

            bool deletionReverted = false;
            if (dbStop.IsDeleted)
            {
              deletionReverted = true;
              dbStop.IsDeleted = false;
            }

            if (IsChanged(stop, dbStop) || deletionReverted)
            {
              ModifyStop(stop, dbStop, dbContext, report, deletionReverted);
            }
          }
        }
        else
        {

          double stopLat = double.Parse(stop.StopLat, CultureInfo.InvariantCulture);
          double stopLon = double.Parse(stop.StopLon, CultureInfo.InvariantCulture);

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
          GtfsStop stopFromFile = stops.FirstOrDefault(s => s.StopId == dbStop.StopId);
          if (stopFromFile == null && dbStop.StopType == StopType.Gtfs)
          {
            await RemoveStop(dbStop, dbContext, report);
          }
        }
      }

      return report;
    }
    private void ModifyStop(GtfsStop stop, DbStop dbStop, ApplicationDbContext dbContext, GtfsImportReport report, bool deletionReverted)
    {
      // Report - new stop
      ReportStop reportStop =
        _reportsFactory.CreateStop(report, dbStop, ChangeAction.Modified, deletionReverted);

      dbStop.Version = 0;

      double stopLat = double.Parse(stop.StopLat, CultureInfo.InvariantCulture);
      double stopLon = double.Parse(stop.StopLon, CultureInfo.InvariantCulture);

      bool isInitLatFilled = dbStop.InitLat != null;
      bool isInitLonFilled = dbStop.InitLon != null;

      if (stopLat != dbStop.Lat && stopLon != dbStop.Lon)
      {
        dbStop.Tile = dbContext.Tiles.FirstOrDefault(tile =>
            stopLat >= tile.MinLat &&
            stopLat <= tile.MaxLat &&
            stopLon >= tile.MinLon &&
            stopLon <= tile.MaxLon
          );
        dbStop.TileId = dbStop.Tile.Id;
      }

      if (stopLat != dbStop.Lat)
      {
        _reportsFactory.AddField(reportStop,
          isInitLatFilled ? nameof(dbStop.InitLat) : nameof(dbStop.Lat), stopLat.ToString(), dbStop.Lat.ToString(), ChangeAction.Modified);

        if (isInitLatFilled)
        {
          dbStop.InitLat = stopLat;
        }
        else
        {
          dbStop.Lat = stopLat;
        }
      }

      if (stopLon != dbStop.Lon)
      {
        _reportsFactory.AddField(reportStop,
          isInitLatFilled ? nameof(dbStop.InitLon) : nameof(dbStop.Lon), stopLon.ToString(), dbStop.Lon.ToString(), ChangeAction.Modified);

        if (isInitLonFilled)
        {
          dbStop.InitLon = stopLon;
        }
        else
        {
          dbStop.Lon = stopLon;
        }
      }

      if (stop.StopName != dbStop.Name)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Name), stop.StopName, dbStop.Name, ChangeAction.Modified);

        dbStop.Name = stop.StopName;
      }

      if (stop.StopCode != dbStop.Number)
      {
        _reportsFactory.AddField(reportStop,
          nameof(dbStop.Number), stop.StopCode, dbStop.Number, ChangeAction.Modified);

        dbStop.Number = stop.StopCode;
      }

      dbContext.Stops.Update(dbStop);
    }

    private void AddStop(DbTile tile, GtfsImportReport report, GtfsStop stop)
    {
      DbStop dbStop = new DbStop
      {
        Name = stop.StopName,
        Number = stop.StopCode,
        StopId = stop.StopId,
        Lat = double.Parse(stop.StopLat, CultureInfo.InvariantCulture),
        Lon = double.Parse(stop.StopLon, CultureInfo.InvariantCulture),
        StopType = StopType.Gtfs,
        ProviderType = ProviderType.Ztm,
        Version = 0,
        TileId = tile.Id,
        Tile = tile,
      };

      ReportStop reportStop = _reportsFactory.CreateStop(report, dbStop, ChangeAction.Added);
      tile.Stops.Add(dbStop);
    }

    private async Task RemoveStop(DbStop stop, ApplicationDbContext dbContext, GtfsImportReport report)
    {
      DbStop dbStop = await dbContext.Stops.FirstOrDefaultAsync(x => x.StopId == stop.StopId);

      if (dbStop == null || dbStop.IsDeleted) return;
      _reportsFactory.CreateStop(report, stop, ChangeAction.Removed);
      dbStop.IsDeleted = true;
      dbContext.Stops.Update(dbStop);
    }
  }
}