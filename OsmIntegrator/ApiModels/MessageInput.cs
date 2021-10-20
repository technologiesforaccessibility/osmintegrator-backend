using System;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.ApiModels
{
  public class MessageInput
  {
    public Guid? ConversationId { get; set; }
    public string Text { get; set; }

    public double? Lat { get; set; }

    public double? Lon { get; set; }

    public Guid? StopId { get; set; }


  public Guid TileId { get; set; }
  }
}