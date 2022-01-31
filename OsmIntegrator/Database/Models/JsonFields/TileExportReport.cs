using System.Collections.Generic;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class TileExportReport
  {
    public List<ConnectionReport> Connections { get; set; } = new();
  }
}