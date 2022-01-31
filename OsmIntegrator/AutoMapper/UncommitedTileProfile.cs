using System.Globalization;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
  public class UncommitedTileProfile : Profile
  {
    public UncommitedTileProfile()
    {
      AllowNullCollections = true;
      CreateMap<DbTile, UncommitedTile>()
          .ForMember(x => x.AssignedUserName, 
            o => o.MapFrom(x => x.AssignedUser == null ? string.Empty : x.AssignedUser.UserName));
    }
  }
}