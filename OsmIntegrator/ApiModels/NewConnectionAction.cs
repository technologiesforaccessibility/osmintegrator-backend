using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
  public class NewConnectionAction
  {
    [Required]
    public Guid? TileId { get; set; }
    [Required]
    public Guid? OsmStopId { get; set; }
    [Required]
    public Guid? GtfsStopId { get; set; }
  }
}