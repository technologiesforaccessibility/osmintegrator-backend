using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Conversation;

public class MessageInput
{
  public Guid? ConversationId { get; set; }

  public string Text { get; set; }

  public double? Lat { get; set; }

  public double? Lon { get; set; }

  public Guid? StopId { get; set; }

  [Required]
  public Guid? TileId { get; set; }
}
