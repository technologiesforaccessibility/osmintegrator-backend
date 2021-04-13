using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class ConnectionProfile : Profile
    {
        public ConnectionProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbConnection, Connection>()
                .ReverseMap();
        }
    }
}