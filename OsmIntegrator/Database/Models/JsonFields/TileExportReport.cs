using System;
using System.Collections.Generic;
using System.Text;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class TileExportReport
  {
    public List<ConnectionReport> Connections { get; set; } = new();
    public Guid TileId { get; set; }
    public long TileX { get; set; }
    public long TileY { get; set; }
  }
}