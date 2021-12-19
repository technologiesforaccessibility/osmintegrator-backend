using System;
using System.Collections.Generic;
using System.Text;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ReportTile
  {
    public List<ReportStop> Stops { get; set; } = new();
    public Guid TileId { get; set; }
    public long TileX { get; set; }
    public long TileY { get; set; }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"[TILE] X: {TileX}, Y: {TileY}");
      sb.AppendLine("");
      if(Stops.Count == 0)
      {
        sb.Append("No changes");
        return sb.ToString();
      }
      Stops.ForEach(x => sb.AppendLine(x.ToString()));
      return sb.ToString();
    }
  }
}