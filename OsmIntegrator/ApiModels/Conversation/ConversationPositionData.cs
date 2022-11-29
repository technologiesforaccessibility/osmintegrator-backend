using System;
using System.ComponentModel.DataAnnotations;

public class ConversationPositionData
{
  [Required]
  public double Lat { get; set; }

  [Required]
  public double Lon { get; set; }

  [Required]
  public Guid? ConversationId { get; set; }
}