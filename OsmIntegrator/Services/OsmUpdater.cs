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

namespace OsmIntegrator.Services
{
  public class OsmUpdater : IOsmUpdater
  {
    private readonly IReportsFactory _reportsFactory;
    private readonly ILogger<IOsmUpdater> _logger;

    public OsmUpdater(IReportsFactory reportsFactory, ILogger<IOsmUpdater> logger)
    {
      _reportsFactory = reportsFactory;
      _logger = logger;
    }

    private void RemoveConnections(ApplicationDbContext dbContext)
    {
      List<DbConnections> connectionsToDelete = dbContext.Connections
        .Include(x => x.OsmStop)
        .Include(x => x.GtfsStop)
        .Where(x => x.OsmStop.IsDeleted == true)
        .ToList();

      dbContext.Connections.RemoveRange(connectionsToDelete);
    }

    public async Task<ReportTile> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
      try
      {
        ReportTile report = ProcessTile(tile, dbContext, osmRoot);

        RemoveConnections(dbContext);

        if (report.Stops.Count > 0)
        {
          dbContext.ChangeReports.Add(new DbChangeReport
          {
            CreatedAt = DateTime.Now,
            TileId = tile.Id,
            TileReport = report
          });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return report;
      }
      catch
      {
        await transaction.RollbackAsync();
        throw;
      }
    }

    public async Task<List<ReportTile>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
      {
        List<ReportTile> reports = new();
        try
        {
          tiles.ForEach(x => reports.Add(ProcessTile(x, dbContext, osmRoot)));

          RemoveConnections(dbContext);

          reports.ForEach(x =>
          {
            if (x.Stops.Count > 0)
            {
              dbContext.ChangeReports.Add(new DbChangeReport
              {
                CreatedAt = DateTime.Now,
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

    private ReportTile ProcessTile(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      // Report - init
      ReportTile report = _reportsFactory.Create(tile);

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

        DbStop existingStop = tile.Stops.FirstOrDefault(
          x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm);

        if (existingStop != null)
        {
          if (existingStop.Changeset == node.Changeset && existingStop.Version == node.Version)
          {
            continue;
          }

          ModifyStop(existingStop, node, dbContext, report);
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

    private void ModifyStop(DbStop existingStop, Node node, ApplicationDbContext dbContext, ReportTile report)
    {
      // Report - new stop
      ReportStop reportStop = _reportsFactory.CreateStop(report, node, existingStop, ChangeAction.Modified);

      existingStop.Version = node.Version;
      existingStop.Changeset = node.Changeset;

      double nodeLat = double.Parse(node.Lat, CultureInfo.InvariantCulture);
      if (existingStop.Lat != nodeLat)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.Lat), nodeLat.ToString(), existingStop.Lat.ToString(), ChangeAction.Modified);

        existingStop.Lat = nodeLat;
      }

      double nodeLong = double.Parse(node.Lon, CultureInfo.InvariantCulture);
      if (existingStop.Lon != nodeLong)
      {
        _reportsFactory.AddField(reportStop,
          nameof(existingStop.Lon), nodeLong.ToString(), existingStop.Lon.ToString(), ChangeAction.Modified);

        existingStop.Lon = nodeLong;
      }

      List<Tag> newTags = PopulateTags(existingStop, node, reportStop);
      UpdateStopProperties(existingStop, newTags, reportStop);
      existingStop.Tags = newTags;
      dbContext.Stops.Update(existingStop);
    }

    private void AddStop(Node node, DbTile tile, ReportTile report)
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
      tile.Stops.Add(stop);
    }

    private void RemoveStop(DbStop stop, ApplicationDbContext dbContext, ReportTile report)
    {
      _reportsFactory.CreateStop(report, null, stop, ChangeAction.Added);
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
  }
}