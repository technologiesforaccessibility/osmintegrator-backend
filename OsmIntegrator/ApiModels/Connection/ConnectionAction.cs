using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels.Connections
{
  public class ConnectionAction
  {
    [Required]
    public Guid? OsmStopId { get; set; }
    [Required]
    public Guid? GtfsStopId { get; set; }
  }
}