using System;
using System.Collections.Generic;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ReportTile
  {
    public List<ReportStop> Stops { get; set; } = new();
    public Guid TileId { get; set; }
    public long TileX { get; set; }
    public long TileY { get; set; }
  }
}