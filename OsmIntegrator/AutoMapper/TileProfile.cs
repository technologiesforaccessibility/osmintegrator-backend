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
            CreateMap<DbTile, Tile>()
                .ForMember(x => x.UsersCount, o => o.MapFrom(x => x.Users.Count))
                .ForMember(x => x.ApprovedBySupervisor, o => o.MapFrom(x => x.SupervisorApproved != null))
                .ForMember(x => x.ApprovedByEditor, o => o.MapFrom(x => x.EditorApproved != null));
        }
    }
}