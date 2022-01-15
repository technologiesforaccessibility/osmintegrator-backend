using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Enums;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Services
{
  public interface IOsmExporter
  {
    Task<string> GetOsmChangeFile(DbTile tile);
    string GetComment(long x, long y, int zoom);
  }

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
    public async Task<string> GetOsmChangeFile(DbTile tile)
    {
      List<DbStop> stops = await _dbContext.Stops
        .Where(x => x.TileId == tile.Id && x.StopType == StopType.Osm && x.OsmConnections.Count > 0)
        .Include(x => x.OsmConnections
          .OrderByDescending(y => y.CreatedAt)
          .Take(1))
        .ThenInclude(x => x.GtfsStop)
        .ToListAsync();

      List<DbConnection> connections = new();
      foreach (DbStop stop in stops)
      {
        DbConnection currentConnection = stop.OsmConnections.First();
        if (currentConnection.OperationType == ConnectionOperationType.Added)
          connections.Add(currentConnection);
      }

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

    public string GetComment(long x, long y, int zoom)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("This change is about updating name, ref and local_ref tags inside bus and tram stops. ");
      sb.Append($"The change affects the tile at: X - {x}; Y - {y}; zoom - {zoom}. ");
      sb.Append("Wiki page: https://wiki.openstreetmap.org/w/index.php?title=Automated_edits/luktar/OsmIntegrator_-_fixing_stop_signs_for_blind, ");
      sb.Append("project page: https://osmintegrator.eu");
      return sb.ToString();
    }
  }
}