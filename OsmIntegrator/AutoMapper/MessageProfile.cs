using System;
using AutoMapper;
using OsmIntegrator.ApiModels.Conversation;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper;

public class MessageProfile : Profile
{
  public MessageProfile()
  {
    AllowNullCollections = true;
    CreateMap<DbMessage, Message>()
      .ForMember(x => x.Username, o => o.MapFrom(x => x.User.UserName))
      .ReverseMap();
  }
}
