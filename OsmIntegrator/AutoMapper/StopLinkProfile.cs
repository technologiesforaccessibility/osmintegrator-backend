using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class StopLinkProfile : Profile
    {
        public StopLinkProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbStopLink, Connection>()
                .ReverseMap();
        }
    }
}