using System.Globalization;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
  public class TileProfile : Profile
  {
    public TileProfile(IConfiguration configuration)
    {
      byte zoomLevel = byte.Parse(configuration["ZoomLevel"], NumberFormatInfo.InvariantInfo);

      AllowNullCollections = true;
      CreateMap<DbTile, Tile>()
          .ForMember(x => x.ZoomLevel, o => o.MapFrom(x => zoomLevel))
          .ForMember(x => x.AssignedUserName, 
            o => o.MapFrom(x => x.AssignedUser == null ? string.Empty : x.AssignedUser.UserName));
    }
  }
}