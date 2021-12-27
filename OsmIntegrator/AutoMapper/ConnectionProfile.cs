using AutoMapper;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
  public class ConnectionProfile : Profile
  {
    public ConnectionProfile()
    {
      AllowNullCollections = true;
      CreateMap<DbConnection, Connection>()
        .ForMember(x => x.Approved, o => o.MapFrom(x => x.ApprovedById != null))
        .ReverseMap();
    }
  }
}