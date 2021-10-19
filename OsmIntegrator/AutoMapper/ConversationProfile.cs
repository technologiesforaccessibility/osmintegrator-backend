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
      CreateMap<DbConversation, Conversation>().ReverseMap();
    }
  }
}