using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
  public class Conversation
  {
    public Guid? Id { get; set; }

    public double? Lat { get; set; }

    public double? Lon { get; set; }

    public Guid? StopId { get; set; }

    [Required]
    public Guid TileId { get; set; }

    public List<Message> Messages { get; set; }

  }
}