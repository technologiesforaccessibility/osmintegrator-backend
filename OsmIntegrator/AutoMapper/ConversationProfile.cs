using System;
using System.Linq;
using AutoMapper;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.AutoMapper
{
  public class ConversationProfile : Profile
  {
    public ConversationProfile()
    {
      AllowNullCollections = true;
      CreateMap<DbConversation, Conversation>()
        .ForMember(
          x => x.Status,
          o => o
            .MapFrom(
              x => x.Messages
              .OrderByDescending(y => y.CreatedAt)
              .FirstOrDefault()
              .Status
            )
        )
        .ReverseMap();
    }
  }
}