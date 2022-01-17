using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Services
{
  public class OsmExporter : IOsmExporter
  {
    public readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    public OsmExporter(ApplicationDbContext dbContext, IMapper mapper, IConfiguration config)
    {
      _dbContext = dbContext;
      _mapper = mapper;
      _config = config;

    }
    public string GetOsmChangeFile(DbTile tile)
    {
      List<DbConnection> connections = tile.ActiveConnections(false).ToList();

      OsmChange root = new OsmChange()
      {
        Generator = "osm integrator v0.1",
        Version = "0.6",
        Mod = new Modify()
        {
          Nodes = new List<Node>()
        }
      };

      foreach (DbConnection connection in connections)
      {
        root.Mod.Nodes.Add(CreateNode(connection.OsmStop, connection.GtfsStop));
      };

      return SerializationHelper.XmlSerialize(root);
    }
    private Node CreateNode(DbStop osmStop, DbStop gtfsStop)
    {
      Node node = new()
      {
        Tag = new List<Tag>(),
        Changeset = osmStop.Changeset,
        Version = osmStop.Version,
        Lat = osmStop.Lat.ToString(),
        Lon = osmStop.Lon.ToString(),
        Id = osmStop.StopId.ToString()
      };

      foreach (var apiTag in osmStop.Tags)
      {
        node.Tag.Add(new Tag()
        {
          K = apiTag.Key,
          V = apiTag.Value
        });
      }

      UpdateTag(node.Tag, Constants.REF, gtfsStop.StopId.ToString());
      UpdateTag(node.Tag, Constants.LOCAL_REF, gtfsStop.Number);
      UpdateTag(node.Tag, Constants.NAME, gtfsStop.Name);

      return node;
    }

    private void UpdateTag(List<Tag> tags, string key, string value)
    {
      Tag tag = tags.FirstOrDefault(x => x.K.ToLower() == key);
      if (tag == null)
      {
        tags.Add(new Tag { K = key, V = value });
        return;
      }

      tag.V = value;
    }

    public string GetChangeset(string comment)
    {
      OsmChangeset changeset = new();

      changeset.Tags = new List<Tag>() {
        new Tag { K = "comment", V = comment },
        new Tag { K = "import", V = "yes"},
        new Tag { K = "created_by", V = "osmintegrator.eu" },
        new Tag { K = "source", V = "Zarząd Transportu Metropolitalnego" },
        new Tag { K = "hashtags", V = "#osmintegrator;#ztm;#silesia" }
      };

      return SerializationHelper.XmlSerialize(changeset);
    }

    public string GetComment(long x, long y, byte zoom)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Updating ref and local_ref with GTFS data. ");
      sb.Append($"Tile X: {x}, Y: {y}, Zoom: {zoom}. ");
      sb.Append("Wiki: https://wiki.openstreetmap.org/w/index.php?title=Automated_edits/luktar/OsmIntegrator_-_fixing_stop_signs_for_blind");
      return sb.ToString();
    }
  }
}