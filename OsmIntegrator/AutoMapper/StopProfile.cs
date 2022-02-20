using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class StopProfile : Profile
    {
        public StopProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbStop, Stop>().ReverseMap();
        }
    }
}