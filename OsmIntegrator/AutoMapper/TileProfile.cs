using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class TileProfile : Profile
    {
        public TileProfile()
        {
            AllowNullCollections = true;
            CreateMap<DbTile, Tile>().ReverseMap();
        }
    }
}