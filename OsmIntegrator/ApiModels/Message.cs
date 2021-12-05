using System;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.ApiModels
{
  public class Message
  {
    public Guid? Id { get; set; }

    public Guid? UserId { get; set; }

    public string Text { get; set; }

    public string Username { get; set; }

    public NoteStatus Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? ConversationId { get; set; }

    public Conversation Conversation { get; set; }
  }
}