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
      CreateMap<DbConnections, Connection>()
        .ForMember(x => x.Approved, o => o.MapFrom(x => x.ApprovedById != null))
        .ReverseMap();
    }
  }
}