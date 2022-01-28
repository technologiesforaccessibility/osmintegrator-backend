using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
    public async Task<OsmChange> GetOsmChangeAsync(Guid tileId, uint changesetId)
    {
      DbTile tile = await _dbContext.Tiles
        .Include(x => x.Stops)
        .ThenInclude(x => x.OsmConnections)
        .Include(t => t.ExportReports)
        .FirstOrDefaultAsync(x => x.Id == tileId);

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
        root.Mod.Nodes.Add(CreateNode(connection.OsmStop, connection.GtfsStop, changesetId));
      };

      return root;
    }
    private Node CreateNode(DbStop osmStop, DbStop gtfsStop, uint changesetId)
    {
      Node node = new()
      {
        Tag = new List<Tag>(),
        Changeset = changesetId.ToString(),
        Version = osmStop.Version,
        Lat = osmStop.Lat.ToString(CultureInfo.InvariantCulture),
        Lon = osmStop.Lon.ToString(CultureInfo.InvariantCulture),
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

    public OsmChangeset CreateChangeset(string comment)
    {
      var tags = GetTags(comment)
        .Select(t => new Tag
        {
          K = t.Key,
          V = t.Value
        })
        .ToList();

      OsmChangeset changeset = new()
      {
        Tags = tags
      };

      return changeset;
    }

    public string GetComment(long x, long y, byte zoom)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Updating ref and local_ref with GTFS data. ");
      sb.Append($"Tile X: {x}, Y: {y}, Zoom: {zoom}. ");
      sb.Append("Wiki: https://wiki.openstreetmap.org/w/index.php?title=Automated_edits/luktar/OsmIntegrator_-_fixing_stop_signs_for_blind");
      return sb.ToString();
    }

    public IReadOnlyDictionary<string, string> GetTags(string comment) => new Dictionary<string, string> {
      {"comment", comment},
      {"import", "yes"},
      {"created_by", "osmintegrator"},
      {"source", "Zarząd Transportu Metropolitalnego"},
      {"hashtags", "#osmintegrator;#ztm;#silesia"}
    };
  }
}