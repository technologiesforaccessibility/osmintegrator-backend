using System;
using System.ComponentModel.DataAnnotations;

public class StopPositionData
{
  [Required]
  public double Lat { get; set; }
  
  [Required]
  public double Lon { get; set; }

  [Required]
  public Guid? StopId { get; set; }
}