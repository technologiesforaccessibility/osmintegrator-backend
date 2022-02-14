using System;

namespace OsmIntegrator.Database.QueryObjects;

public class UncommittedTileQuery
{
  public Guid Id { get; set; }
  public long X { get; set; }
  public long Y { get; set; }
  public double MaxLat { get; set; }
  public double MinLon { get; set; }
  public double MinLat { get; set; }
  public double MaxLon { get; set; }
  public int GtfsStopsCount { get; set; }
}