using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
  public class TileUser
  {
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string UserName { get; set; }

    [Required]
    public bool IsAssigned { get; set; }

    [Required]
    public bool IsAssignedAsSupervisor { get; set; }

    [Required]
    public bool IsSupervisor { get; set; }
    
    [Required]
    public bool IsEditor { get; set; }
  }
}