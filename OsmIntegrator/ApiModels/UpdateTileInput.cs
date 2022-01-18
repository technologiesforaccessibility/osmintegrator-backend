using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
  public class UpdateTileInput
  {
    [Required]
    public Guid? EditorId { get; set; }
  }
}