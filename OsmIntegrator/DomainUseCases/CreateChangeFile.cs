using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Enums;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;

namespace OsmIntegrator.DomainUseCases
{

    public class CreateChangeFileInputDto
    {
        public Guid TileUuid { get; }
        public CreateChangeFileInputDto(Guid tileUuid)
        {
            TileUuid = tileUuid;
        }

    }

    public class CreateChangeFileResponse : AUseCaseResponse
    {
        public MemoryStream XmlStream { get; private set; }
        public CreateChangeFileResponse(string message, MemoryStream xmlStream,
                                        IEnumerable<string> errors = null) : base(message, errors)
        {
            XmlStream = xmlStream;
        }
    }

    public class CreateChangeFile : IUseCase<CreateChangeFileInputDto>
    {
        public readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        public CreateChangeFile(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;

        }
        public async Task<AUseCaseResponse> Handle(CreateChangeFileInputDto useCaseData)
        {                    
            var querySet = await _dbContext.Connections.Where(connection => connection.OsmStop.Tile.Id == useCaseData.TileUuid && connection.OperationType == ConnectionOperationType.Added)
                .Include(connection => connection.GtfsStop)
                .Include(connection => connection.OsmStop)                    
                .OrderByDescending(con => con.CreatedAt)
                .ThenByDescending(con => con.OsmStop)
                .ThenByDescending(con => con.GtfsStop)                    
                .ToListAsync();
            
            var existingConnections = _mapper.Map<List<Connection>>(querySet.Distinct(new DbStopLinkComparer()).ToList());
                    
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
                osmNodes.Mod.Nodes.Add(createNode(connection.OsmStop, connection.GtfsStop));
            };
            return new CreateChangeFileResponse(xmlStream: createModifyChengeFile(osmNodes), message: "Osm change created");
        }
        private Tools.Node createNode(Stop osmStop, Stop gtfsStop)
        {
            var node = new Tools.Node(){
                Tag = new List<Tools.Tag>(),
                Changeset = osmStop.Changeset,
                Version = osmStop.Version,
                Lat = osmStop.Lat.ToString(),
                Lon = osmStop.Lon.ToString(),
                Id = osmStop.StopId.ToString()
                
            };
            foreach (var apiTag in osmStop.Tags)
            {
                if (apiTag.Key == "ref")
                {
                    node.Tag.Add(new Tools.Tag()
                    {
                        K = apiTag.Key,
                        V = osmStop.StopId.ToString()
                    });
                }
                else if (apiTag.Key == "local_ref")
                {
                    node.Tag.Add(new Tools.Tag()
                    {
                        K = apiTag.Key,
                        V = osmStop.Number
                    });
                }
                else if (apiTag.Key == "name")
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
        private MemoryStream createModifyChengeFile(OsmChange changeNode)
        {
            var stream = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(Tools.OsmChange));
            serializer.Serialize(stream, changeNode);            
            stream.Position = 0;            
            return stream;

        }
    }
}