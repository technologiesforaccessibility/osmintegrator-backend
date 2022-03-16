using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Controllers;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class TileImportReport
  {
    public List<ReportStop> Stops { get; set; } = new();
    public Guid TileId { get; set; }
    public long TileX { get; set; }
    public long TileY { get; set; }

    public string GetResultText(IStringLocalizer<TileController> localizer = null)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"[{localizer["TILE"]}] X: {TileX}, Y: {TileY}");
      sb.AppendLine("");
      if (Stops.Count == 0)
      {
        sb.Append(localizer["No changes"]);
        return sb.ToString();
      }
      Stops.ForEach(x => sb.AppendLine(x.GetResultText(localizer)));
      return sb.ToString();
    }
  }
}