using System;
using System.Collections.Generic;

namespace OsmIntegrator.ApiModels.Conversation
{
  public class ConversationResponse
  {
    public List<Conversation> StopConversations { get; set; }

    public List<Conversation> GeoConversations { get; set; }

  }
}