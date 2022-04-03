using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Controllers;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class GtfsImportReport
  {
    public List<ReportStop> Stops { get; set; } = new();

    public string GetResultText(IStringLocalizer<TileController> localizer)
    {
      StringBuilder sb = new StringBuilder();
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