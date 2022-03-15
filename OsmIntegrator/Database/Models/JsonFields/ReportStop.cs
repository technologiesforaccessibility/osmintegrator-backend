using System;
using System.Collections.Generic;
using System.Text;
using OsmIntegrator.Database.Models.Enums;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Controllers;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ReportStop
  {
    public string Name { get; set; }
    public string PreviousName { get; set; }
    public int Version { get; set; }
    public int? PreviousVersion { get; set; }
    public string Changeset { get; set; }
    public string PreviousChangeset { get; set; }
    public string StopId { get; set; }
    public StopType StopType { get; set; }
    public Guid? DatabaseStopId { get; set; }
    public List<ReportField> Fields { get; set; }
    public ChangeAction Action { get; set; }
    public bool Reverted { get; set; }

    private string GetResultText(IStringLocalizer<TileController> localizer = null)
    {
      StringBuilder sb = new StringBuilder();
      string previousName = !string.IsNullOrEmpty(PreviousName) ? $" ({PreviousName})" : "";
      string previousVersion = PreviousVersion is not null ? $" ({PreviousVersion.ToString()})" : "";
      string reverted = Reverted ? ", Reverted" : "";

      if (localizer != null)
      {
        sb.AppendLine($"[{localizer["STOP"]}-{localizer[Action.ToString()]}] {Name}{previousName}, Id: {StopId}, {StopType.ToString()}, {localizer["Ver"]}: {Version}{previousVersion}{localizer[reverted]}");
      }
      else
      {
        sb.AppendLine($"[STOP-{Action.ToString()}] {Name}{previousName}, Id: {StopId}, {StopType.ToString()}, Ver: {Version}{previousVersion}{reverted}");
      }


      Fields?.ForEach(x => sb.AppendLine(x.GetTranslatedResultText(localizer)));

      return sb.ToString();
    }

    public string GetTranslatedResultText(IStringLocalizer<TileController> localizer = null) => GetResultText(localizer);

    public override string ToString() => GetResultText();
  }
}