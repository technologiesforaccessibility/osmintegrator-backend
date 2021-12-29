using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
      return CreateChangeFile(root);
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
        if (apiTag.Key == Constants.REF)
        {
          node.Tag.Add(new Tag()
          {
            K = apiTag.Key,
            V = gtfsStop.StopId.ToString()
          });
        }
        else if (apiTag.Key == Constants.LOCAL_REF)
        {
          node.Tag.Add(new Tag()
          {
            K = apiTag.Key,
            V = osmStop.Number
          });
        }
        else if (apiTag.Key == Constants.NAME)
        {
          node.Tag.Add(new Tag()
          {
            K = apiTag.Key,
            V = gtfsStop.Name
          });
        }
        else
        {
          node.Tag.Add(new Tag()
          {
            K = apiTag.Key,
            V = apiTag.Value
          });
        }
      }

      if (!node.Tag.Any(x => x.K.ToLower() == Constants.REF))
      {
        node.Tag.Add(new Tag()
        {
          K = Constants.REF,
          V = gtfsStop.StopId.ToString()
        });
      }
      if (!node.Tag.Any(x => x.K.ToLower() == Constants.LOCAL_REF))
      {
        node.Tag.Add(new Tag()
        {
          K = Constants.LOCAL_REF,
          V = osmStop.Number
        });
      }
      if (!node.Tag.Any(x => x.K.ToLower() == Constants.NAME))
      {
        node.Tag.Add(new Tag()
        {
          K = Constants.NAME,
          V = gtfsStop.Name
        });
      }

      return node;
    }

    private string CreateChangeFile(OsmChange changeNode)
    {
      XmlSerializer serializer = new XmlSerializer(typeof(OsmChange));
      using StringWriter textWriter = new StringWriter();
      serializer.Serialize(textWriter, changeNode);
      return textWriter.ToString();
    }

    public string GetComment(long x, long y, int zoom)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("This change is about update tags inside bus and tram stops.");
      sb.AppendLine("Updated tags are: name, ref and local_ref");
      sb.AppendLine("The change file is generated automatically by the soft called OsmIntegrator.");
      sb.AppendLine("See the wiki page: https://wiki.openstreetmap.org/wiki/OsmIntegrator");
      sb.AppendLine("And the soft itself: https://osmintegrator.eu");
      sb.AppendLine($"The change affects the tile at: X - {x}; Y - {y}; zoom - {zoom}");
      return sb.ToString();
    }
  }
}