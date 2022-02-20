using System.Globalization;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
  public class UncommittedTileProfile : Profile
  {
    public UncommittedTileProfile()
    {
      AllowNullCollections = true;
      CreateMap<DbTile, UncommittedTile>()
          .ForMember(x => x.AssignedUserName, 
            o => o.MapFrom(x => x.AssignedUser == null ? string.Empty : x.AssignedUser.UserName));
    }
  }
}