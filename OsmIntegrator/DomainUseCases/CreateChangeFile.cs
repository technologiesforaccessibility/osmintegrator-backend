using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Enums;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;

namespace OsmIntegrator.DomainUseCases
{

  public class CreateChangeFileInputDto
  {
    public Guid TileId { get; }
    public CreateChangeFileInputDto(Guid tileId)
    {
      TileId = tileId;
    }

  }

  public class CreateChangeFileResponse : AUseCaseResponse
  {
    public MemoryStream XmlStream { get; private set; }
    public MemoryStream CommentStream { get; private set; }
    public CreateChangeFileResponse(string message, MemoryStream xmlStream, MemoryStream commentStream,
                                    IEnumerable<string> errors = null) : base(message, errors)
    {
      XmlStream = xmlStream;
      CommentStream = commentStream;
    }
  }

  public class CreateChangeFile : IUseCase<CreateChangeFileInputDto>
  {
    public readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    public CreateChangeFile(ApplicationDbContext dbContext, IMapper mapper, IConfiguration config)
    {
      _dbContext = dbContext;
      _mapper = mapper;
      _config = config;

    }
    public async Task<AUseCaseResponse> Handle(CreateChangeFileInputDto useCaseData)
    {
      var querySet = await _dbContext.Connections.Where(x =>
            x.OsmStop.Tile.Id == useCaseData.TileId &&
            x.OperationType == ConnectionOperationType.Added)
          .Include(connection => connection.GtfsStop)
          .Include(connection => connection.OsmStop)
          .OrderByDescending(con => con.CreatedAt)
          .ThenByDescending(con => con.OsmStop)
          .ThenByDescending(con => con.GtfsStop)
          .ToListAsync();

      DbTile tile = await _dbContext.Tiles
        .FirstAsync(t => t.Id == useCaseData.TileId);

      List<DbStop> stops = await _dbContext.Stops
        .Where(x => x.TileId == tile.Id && x.StopType == StopType.Osm)
        .Include(x => x.OsmConnections
          .OrderByDescending(y => y.CreatedAt)
          .Take(1))
        // .ThenInclude(x => x.GtfsStop)
        .ToListAsync();

      List<DbConnection> a;

      foreach (DbStop stop in stops)
      {

      }



      List<Connection> existingConnections =
        _mapper.Map<List<Connection>>(querySet.Distinct(new DbConnectionComparer()).ToList());

      OsmChange osmNodes = new OsmChange()
      {
        Generator = "osm integrator v0.1",
        Version = "0.6",
        Mod = new Modify()
        {
          Nodes = new List<Tools.Node>()
        }
      };
      foreach (var connection in existingConnections)
      {
        osmNodes.Mod.Nodes.Add(CreateNode(connection.OsmStop, connection.GtfsStop));
      };
      return new CreateChangeFileResponse(commentStream: CreateCommentFile(tile.X, tile.Y, int.Parse(_config["ZoomLevel"])),
                                          xmlStream: CreateModifyChangeFile(osmNodes),
                                          message: "Osm change created");
    }
    private Tools.Node CreateNode(Stop osmStop, Stop gtfsStop)
    {
      var node = new Tools.Node()
      {
        Tag = new List<Tools.Tag>(),
        Changeset = osmStop.Changeset,
        Version = osmStop.Version,
        Lat = osmStop.Lat.ToString(),
        Lon = osmStop.Lon.ToString(),
        Id = osmStop.StopId.ToString()
      };
      foreach (var apiTag in osmStop.Tags)
      {
        if (apiTag.Key == Constants.NAME)
        {
          node.Tag.Add(new Tools.Tag()
          {
            K = apiTag.Key,
            V = osmStop.StopId.ToString()
          });
        }
        else if (apiTag.Key == Constants.LOCAL_REF)
        {
          node.Tag.Add(new Tools.Tag()
          {
            K = apiTag.Key,
            V = osmStop.Number
          });
        }
        else if (apiTag.Key == Constants.NAME)
        {
          node.Tag.Add(new Tools.Tag()
          {
            K = apiTag.Key,
            V = gtfsStop.Name
          });
        }
        else
        {
          node.Tag.Add(new Tools.Tag()
          {
            K = apiTag.Key,
            V = apiTag.Value
          });
        }
      }
      return node;
    }
    private MemoryStream CreateModifyChangeFile(OsmChange changeNode)
    {
      var stream = new MemoryStream();
      XmlSerializer serializer = new XmlSerializer(typeof(Tools.OsmChange));
      serializer.Serialize(stream, changeNode);
      stream.Position = 0;
      return stream;

    }
    private MemoryStream CreateCommentFile(long x, long y, int zoom)
    {
      var stream = new MemoryStream();
      StreamWriter writer = new StreamWriter(stream);
      writer.WriteLine("This change is about update tags inside bus and tram stops.");
      writer.WriteLine("Updated tags are: name, ref and local_ref");
      writer.WriteLine("The change file is generated automatically by the soft called OsmIntegrator.");
      writer.WriteLine("See the wiki page: https://wiki.openstreetmap.org/wiki/OsmIntegrator");
      writer.WriteLine("And the soft itself: https://osmintegrator.eu");
      writer.WriteLine($"The change affects the tile at: X - {x}; Y - {y}; zoom - {zoom}");
      writer.Flush();
      stream.Position = 0;
      return stream;
    }
  }
}