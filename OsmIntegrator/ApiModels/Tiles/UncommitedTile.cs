using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Tiles
{
  public class UncommitedTile
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
    public int GtfsStopsCount { get; set; }
    public string AssignedUserName { get; set; }
    public int UnconnectedGtfsStopsCount { get; set; }
  }
}