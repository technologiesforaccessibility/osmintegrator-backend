using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;
using Tag = OsmIntegrator.Database.Models.Tag;

namespace OsmIntegrator.Services
{
  public class OsmRefresherHelper : IOsmRefresherHelper
  {
    public async Task Refresh(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      ProcessTile(tile, dbContext, osmRoot);
      await dbContext.SaveChangesAsync();
    }

    public async Task Refresh(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot)
    {
      foreach (DbTile tile in tiles)
      {
        ProcessTile(tile, dbContext, osmRoot);
      }
      await dbContext.SaveChangesAsync();
    }

    private void ProcessTile(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
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
        DbStop existingStop = tile.Stops.FirstOrDefault(
          x => x.StopId == long.Parse(node.Id) && x.StopType == StopType.Osm);

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

          PopulateTags(stop, node);

          tile.Stops.Add(stop);
        }
      }

      foreach (DbStop stop in tile.Stops)
      {
        if (stop.StopType == StopType.Osm && !osmRoot.Node.Exists(x => long.Parse(x.Id) == stop.StopId))
        {
          stop.IsDeleted = true;
          dbContext.Stops.Update(stop);
        }
      }
    }

    private void PopulateTags(DbStop stop, Node node)
    {
      List<Tag> tempTags = new List<Tag>();

      node.Tag.ForEach(x => tempTags.Add(new Tag()
      {
        Key = x.K,
        Value = x.V
      }));
      stop.Tags = tempTags;

      var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.NAME);
      stop.Name = nameTag?.Value;

      var refTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.REF);
      long refVal;
      if (refTag != null && long.TryParse(refTag.Value, out refVal))
      {
        stop.Ref = refVal;
      }

      var localRefTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.LOCAL_REF);
      if (localRefTag != null && !string.IsNullOrEmpty(localRefTag.Value))
      {
        stop.Number = localRefTag.Value;
      }
    }

  }
}