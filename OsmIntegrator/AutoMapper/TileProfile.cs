using System.Globalization;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
    public class TileProfile : Profile
    {
        public TileProfile(IConfiguration configuration)
        {
            var zoomLevel = byte.Parse(configuration["ZoomLevel"], NumberFormatInfo.InvariantInfo);

            AllowNullCollections = true;
            CreateMap<DbTile, Tile>()
                .ForMember(x => x.UsersCount, o => o.MapFrom(x => x.TileUsers.Count))
                .ForMember(x => x.ApprovedBySupervisor, o => o.MapFrom(x => x.SupervisorApprovedId != null))
                .ForMember(x => x.ZoomLevel, o => o.MapFrom(x => zoomLevel))
                .ForMember(x => x.ApprovedByEditor, o => o.MapFrom(x => x.EditorApprovedId != null));
        }
    }
}