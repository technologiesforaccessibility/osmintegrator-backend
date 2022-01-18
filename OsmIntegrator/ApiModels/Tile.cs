using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.ApiModels.Stops;

namespace OsmIntegrator.ApiModels
{
  public class Tile
  {
    [Required]
    public Guid Id { get; set; }
    [Required]
    public long X { get; set; }
    [Required]
    public long Y { get; set; }
    [Required]
    public double MaxLat { get; set; }
    [Required]
    public double MinLon { get; set; }
    [Required]
    public double MinLat { get; set; }
    [Required]
    public double MaxLon { get; set; }
    [Required]
    public double OverlapMaxLat { get; set; }
    [Required]
    public double OverlapMinLon { get; set; }
    [Required]
    public double OverlapMinLat { get; set; }
    [Required]
    public double OverlapMaxLon { get; set; }
    public int OsmStopsCount { get; set; } = 0;
    public int GtfsStopsCount { get; set; } = 0;
    public int? UsersCount { get; set; }
    public List<Stop> Stops { get; set; }
    [Required]
    public bool ApprovedByEditor { get; set; } = false;
    [Required]
    public bool ApprovedBySupervisor { get; set; } = false;
    public byte ZoomLevel { get; set; }
    public string AssignedUserName { get; set; }
  }
}