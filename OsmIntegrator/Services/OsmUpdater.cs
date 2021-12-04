using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;
using OsmIntegrator.ApiModels.Reports;
using Tag = OsmIntegrator.Database.Models.Tag;

namespace OsmIntegrator.Services
{
  public class OsmUpdater : IOsmUpdater
  {
    private readonly IReportsFactory _reportsFactory;

    public OsmUpdater(IReportsFactory reportsFactory)
    {
      _reportsFactory = reportsFactory;
    }

    public async Task<TileReport> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      TileReport result = ProcessTile(tile, dbContext, osmRoot);
      await dbContext.SaveChangesAsync();
      return result;
    }

    public async Task<List<TileReport>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      List<TileReport> result = new();
      foreach (DbTile tile in tiles)
      {
        result.Add(ProcessTile(tile, dbContext, osmRoot));
      }
      await dbContext.SaveChangesAsync();
      return result;
    }

    private TileReport ProcessTile(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      // Report - init
      TileReport report = _reportsFactory.Create(tile);

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
              nameof(existingStop.Lon), nodeLong.ToString(), existingStop.Lon.ToString(), ChangeAction.Added);

            existingStop.Lon = nodeLong;
          }

          PopulateTags(existingStop, node, reportStop);
          dbContext.Stops.Update(existingStop);
        }
        else
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
          };

          ReportStop reportStop = _reportsFactory.CreateStop(report, node, stop, ChangeAction.Added);

          PopulateTags(stop, node, reportStop);

          tile.Stops.Add(stop);
        }
      }

      foreach (DbStop stop in tile.Stops)
      {
        if (stop.StopType == StopType.Osm && !osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.StopId))
        {
          _reportsFactory.CreateStop(report, null, stop, ChangeAction.Added);

          stop.IsDeleted = true;
          dbContext.Stops.Update(stop);
        }
      }

      return report;
    }

    private List<Tag> FillTags(Node node)
    {
      List<Tag> result = new List<Tag>();

      node.Tag.ForEach(x => result.Add(new Tag()
      {
        Key = x.K,
        Value = x.V
      }));

      return result;
    }

    private void PopulateTags(DbStop stop, Node node, ReportStop reportStop)
    {
      List<Tag> nodeTags = FillTags(node);
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
        }
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

      stop.Tags = newTags;

      Tag nameTag = stop.Tags.FirstOrDefault(x => x.Key.ToLower() == Constants.NAME);
      if (stop.Name != nameTag?.Value)
      {
        _reportsFactory.UpdateName(reportStop, nameTag?.Value);

        // Set or update OSM stop name
        stop.Name = nameTag?.Value;
      }

      Tag refTag = stop.Tags.FirstOrDefault(x => x.Key.ToLower() == Constants.REF);
      long refVal = long.Parse(refTag?.Value);
      if (refVal != stop.Ref)
      {
        // Ref updated
        stop.Ref = refVal;
      }

      Tag localRefTag = stop.Tags.FirstOrDefault(x => x.Key.ToLower() == Constants.LOCAL_REF);
      if (localRefTag?.Value != stop.Number)
      {
        // Update local ref
        stop.Number = localRefTag.Value;
      }
    }
  }
}