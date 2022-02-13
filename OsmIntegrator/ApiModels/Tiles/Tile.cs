using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Tiles
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
    public int GtfsStopsCount { get; set; }
    public byte ZoomLevel { get; set; }
    public string AssignedUserName { get; set; }
    public int UnconnectedGtfsStopsCount { get; set; }
  }
}