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
using Microsoft.Extensions.Configuration;

namespace OsmIntegrator.Services
{
  public class OsmUpdater : IOsmUpdater
  {
    private readonly IReportsFactory _reportsFactory;
    private readonly ILogger<IOsmUpdater> _logger;
    private readonly int _zoomLevel;

    public OsmUpdater(IReportsFactory reportsFactory, ILogger<IOsmUpdater> logger, IConfiguration configuration)
    {
      _reportsFactory = reportsFactory;
      _logger = logger;
      _zoomLevel = int.Parse(configuration["ZoomLevel"]);
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

    /// <summary>
    /// Check if there were any changes on this tile in OSM
    /// </summary>
    /// <param name="tile">Current tile</param>
    /// <param name="osmRoot">OSM file as an object structure</param>
    /// <returns>True if there were changes on that tile</returns>
    public bool ContainsChanges(DbTile tile, Osm osmRoot)
    {
      foreach (Node node in osmRoot.Node)
      {
        if (!IsNodeOnTile(tile, node)) continue;

        DbStop existingStop = tile.Stops.FirstOrDefault(
          x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm);

        if (existingStop != null)
        {
          if (existingStop.Changeset == node.Changeset && existingStop.Version == node.Version)
          {
            continue;
          }

          // Stop modified
          return true;
        }
        // Stop needs to be added
        return true;
      }

      foreach (DbStop stop in tile.Stops)
      {
        if (stop.StopType == StopType.Osm && !osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.StopId))
        {
          // Stop removed
          if (!stop.IsDeleted) return true;
        }
      }

      // Nothing has changed
      return false;
    }

    public async Task<TileImportReport> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
      try
      {
        TileImportReport report = ProcessTile(tile, dbContext, osmRoot);
        await dbContext.SaveChangesAsync();
        RemoveConnections(dbContext);

        if (report.Stops.Count > 0)
        {
          dbContext.ChangeReports.Add(new DbTileImportReport
          {
            CreatedAt = DateTime.Now.ToUniversalTime(),
            TileId = tile.Id,
            TileReport = report
          });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return report;
      }
      catch (Exception e)
      {
        await transaction.RollbackAsync();
        _logger.LogError(e, $"Problem with updating tile with id {tile.Id}");
        throw;
      }
    }

    public async Task<List<TileImportReport>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
      {
        List<TileImportReport> reports = new();
        try
        {
          tiles.ForEach(x => reports.Add(ProcessTile(x, dbContext, osmRoot)));
          await dbContext.SaveChangesAsync();
          RemoveConnections(dbContext);

          reports.ForEach(x =>
          {
            if (x.Stops.Count > 0)
            {
              dbContext.ChangeReports.Add(new DbTileImportReport
              {
                CreatedAt = DateTime.Now.ToUniversalTime(),
                TileId = x.TileId, // Tile id was saved during the report creation
                TileReport = x
              });
            }
          });

          await dbContext.SaveChangesAsync();
          await transaction.CommitAsync();
          return reports;
        }
        catch
        {
          await transaction.RollbackAsync();
          throw;
        }
      }
    }

    private bool IsNodeOnTile(DbTile tile, Node node)
    {
      return !(tile.MinLat > double.Parse(node.Lat, CultureInfo.InvariantCulture)
        || tile.MaxLat < double.Parse(node.Lat, CultureInfo.InvariantCulture)
        || tile.MinLon > double.Parse(node.Lon, CultureInfo.InvariantCulture)
        || tile.MaxLon < double.Parse(node.Lon, CultureInfo.InvariantCulture));
    }

    private TileImportReport ProcessTile(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      // Report - init
      TileImportReport report = _reportsFactory.Create(tile);

      foreach (Node node in osmRoot.Node)
      {
        bool isOnTile = IsNodeOnTile(tile, node);

        if (!isOnTile) continue;

        DbStop existingStop = tile.Stops?.FirstOrDefault(
          x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm);

        if (existingStop == null && isOnTile)
        {
          DbTile otherTile = dbContext.Tiles
            .Include(t => t.Stops)
            .FirstOrDefault(t => t.Stops != null && t.Stops.Count() > 0 && t.Stops.Any(s => s.StopId == long.Parse(node.Id)));
          existingStop = otherTile?.Stops?.FirstOrDefault(s => s.StopId == long.Parse(node.Id));
        }

        if (existingStop != null)
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
                report, node, existingStop, ChangeAction.Modified, deletionReverted);
            }
            continue;
          }

          ModifyStop(existingStop, node, dbContext, report, deletionReverted);
        }
        else
        {
          AddStop(node, tile, report);
        }
      }

      foreach (DbStop stop in tile.Stops)
      {
        if (stop.StopType == StopType.Osm && !osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.StopId))
        {
          RemoveStop(stop, dbContext, report);
        }
      }

      return report;
    }

    private void ModifyStop(DbStop existingStop, Node node, ApplicationDbContext dbContext, TileImportReport report, bool deletionReverted)
    {
      // Report - new stop
      ReportStop reportStop =
        _reportsFactory.CreateStop(report, node, existingStop, ChangeAction.Modified, deletionReverted);

      existingStop.Version = node.Version;
      existingStop.Changeset = node.Changeset;

      double nodeLat = double.Parse(node.Lat, CultureInfo.InvariantCulture);
      double nodeLon = double.Parse(node.Lon, CultureInfo.InvariantCulture);

      if (existingStop.Lat != nodeLat || existingStop.Lon != nodeLon)
      {
        existingStop.Tile = dbContext.Tiles.FirstOrDefault(tile =>
            nodeLat >= tile.MinLat &&
            nodeLat <= tile.MaxLat &&
            nodeLon >= tile.MinLon &&
            nodeLon <= tile.MaxLon
          );
        existingStop.TileId = existingStop.Tile.Id;
      }

      if (existingStop.Lat != nodeLat)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.Lat), nodeLat.ToString(), existingStop.Lat.ToString(), ChangeAction.Modified);

        existingStop.Lat = nodeLat;
      }

      if (existingStop.Lon != nodeLon)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.Lon), nodeLon.ToString(), existingStop.Lon.ToString(), ChangeAction.Modified);

        existingStop.Lon = nodeLon;
      }

      List<Tag> newTags = PopulateTags(existingStop, node, reportStop);
      UpdateStopProperties(existingStop, newTags, reportStop);
      existingStop.Tags = newTags;
      dbContext.Stops.Update(existingStop);
    }

    private void AddStop(Node node, DbTile tile, TileImportReport report)
    {
      DbStop stop = new DbStop
      {
        StopId = long.Parse(node.Id),
        Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
        Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
        StopType = StopType.Osm,
        ProviderType = ProviderType.Ztm,
        Version = node.Version,
        Changeset = node.Changeset,
        TileId = tile.Id,
        Tile = tile,
        Tags = new List<Tag>()
      };

      ReportStop reportStop = _reportsFactory.CreateStop(report, node, stop, ChangeAction.Added);
      List<Tag> newTags = PopulateTags(stop, node, reportStop);
      UpdateStopProperties(stop, newTags, reportStop);
      stop.Tags = newTags;
      tile.Stops ??= new List<DbStop>();
      tile.Stops.Add(stop);
    }

    private void RemoveStop(DbStop stop, ApplicationDbContext dbContext, TileImportReport report)
    {
      if (stop.IsDeleted) return;
      _reportsFactory.CreateStop(report, null, stop, ChangeAction.Removed);
      stop.IsDeleted = true;
      dbContext.Stops.Update(stop);
    }

    private List<Tag> PopulateTags(DbStop stop, Node node, ReportStop reportStop)
    {
      List<Tag> nodeTags = new();
      node.Tag.ForEach(x => nodeTags.Add(new Tag { Key = x.K, Value = x.V }));

      List<Tag> newTags = new List<Tag>();

      // Check for new and updated tags
      foreach (Tag nodeTag in nodeTags)
      {
        string tagName = nodeTag.Key.ToLower();

        Tag dbTag = stop.Tags.FirstOrDefault(
          x => tagName == x.Key.ToLower());

        if (dbTag == null)
        {
          // Report new tag
          _reportsFactory.AddField(reportStop, tagName, nodeTag.Value, null, ChangeAction.Added);

          newTags.Add(nodeTag);
          continue;
        }

        if (nodeTag.Value != dbTag.Value)
        {
          // Report tag modified
          _reportsFactory.AddField(reportStop, tagName, nodeTag.Value, dbTag.Value, ChangeAction.Modified);

          newTags.Add(nodeTag);
          continue;
        }
        newTags.Add(nodeTag);
      };

      // Check for removed tags
      foreach (Tag dbTag in stop.Tags)
      {
        Tag nodeTag = nodeTags.FirstOrDefault(x => x.Key.ToLower() == dbTag.Key.ToLower());
        if (nodeTag == null)
        {
          // Report tag deleted
          _reportsFactory.AddField(reportStop, dbTag.Key.ToLower(), null, null, ChangeAction.Removed);
        }
      }

      return newTags;
    }

    private void UpdateStopProperties(DbStop stop, List<Tag> newTags, ReportStop reportStop)
    {
      Tag nameTag = newTags.FirstOrDefault(x => x.Key.ToLower() == Constants.NAME);
      if (nameTag != null && stop.Name != nameTag.Value)
      {
        _reportsFactory.UpdateName(reportStop, nameTag?.Value);

        // Set or update OSM stop name
        stop.Name = nameTag.Value;
      }

      Tag refTag = newTags.FirstOrDefault(x => x.Key.ToLower() == Constants.REF);
      if (refTag != null && refTag.Value != stop.Ref)
      {
        // Ref updated
        stop.Ref = refTag.Value;
      }

      Tag localRefTag = newTags.FirstOrDefault(x => x.Key.ToLower() == Constants.LOCAL_REF);
      if (localRefTag != null && localRefTag.Value != stop.Number)
      {
        // Update local ref
        stop.Number = localRefTag.Value;
      }
    }

    public async Task UpdateTileReferences(List<DbTile> tiles, ApplicationDbContext dbContext)
    {
      using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
      {

        try
        {
          foreach (DbTile tile in tiles)
          {
            foreach (DbStop stop in tile.Stops)
            {
              double stopLat = stop.Lat;
              double stopLon = stop.Lon;

              Point<long> tileCoordinates = TilesHelper.WorldToTilePos(stopLon, stopLat, _zoomLevel);

              if (tile.X != tileCoordinates.X && tile.Y != tileCoordinates.Y)
              {
                stop.Tile = dbContext.Tiles.FirstOrDefault(t => t.X == tileCoordinates.X && t.Y == tileCoordinates.Y);
                stop.TileId = stop.Tile.Id;
              }
            }
          }

          await dbContext.SaveChangesAsync();
          await transaction.CommitAsync();
        }
        catch (System.Exception)
        {
          await transaction.RollbackAsync();
          throw;
        }
      }
    }
  }
}