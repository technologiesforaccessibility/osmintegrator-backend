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
  public class OsmUpdater : IOsmUpdater
  {
    public async Task Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot)
    {
      ProcessTile(tile, dbContext, osmRoot);
      await dbContext.SaveChangesAsync();
    }

    public async Task Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot)
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

          double nodeLat = double.Parse(node.Lat, CultureInfo.InvariantCulture);
          if (existingStop.Lat != nodeLat)
          {
            // Update lat
            existingStop.Lat = nodeLat;
          }

          double nodeLong = double.Parse(node.Lon, CultureInfo.InvariantCulture);
          if (existingStop.Lon != nodeLong)
          {
            // Update long
            existingStop.Lon = nodeLong;
          }

          // Update version
          existingStop.Version = node.Version;

          // Update changeset
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
      List<Tag> nodeTags = new List<Tag>();

      node.Tag.ForEach(x => nodeTags.Add(new Tag()
      {
        Key = x.K,
        Value = x.V
      }));

      List<Tag> newTags = new List<Tag>();

      // Check for new and updated tags
      foreach (Tag nodeTag in nodeTags)
      {
        Tag dbTag = stop.Tags.FirstOrDefault(
          dbTag => nodeTag.Key.ToLower() == dbTag.Key.ToLower());

        if (dbTag == null)
        {
          // New tag added
          newTags.Add(nodeTag);
          continue;
        }

        if (nodeTag.Value != dbTag.Value)
        {
          // Value updated
          newTags.Add(nodeTag);
        }
      };

      // Check for removed tags
      foreach(Tag dbTag in stop.Tags)
      {
        Tag nodeTag = nodeTags.FirstOrDefault(x => x.Key.ToLower() == dbTag.Key.ToLower());
        if(nodeTag == null)
        {
          // Tag removed
        }
      }

      stop.Tags = newTags;

      Tag nameTag = stop.Tags.FirstOrDefault(x => x.Key.ToLower() == Constants.NAME);
      if(stop.Name != nameTag?.Value)
      {
        // Name updated
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