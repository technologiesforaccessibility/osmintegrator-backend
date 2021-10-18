using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsmIntegrator.Database.Models
{
  [Table("Messages")]
  public class DbMessage
  {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public DbConversation Conversation { get; set; }

    public string Text { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public ApplicationUser User { get; set; }

    [Required]
    public NoteStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

  }
}
