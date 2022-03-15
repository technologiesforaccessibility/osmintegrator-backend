using System.Text;
using OsmIntegrator.Database.Models.Enums;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Controllers;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ReportField
  {
    public string Name { get; set; }
    public string ActualValue { get; set; }
    public string PreviousValue { get; set; }
    public ChangeAction Action { get; set; }

    private string GetResultText(IStringLocalizer<TileController> localizer = null)
    {
      string previousValue = !string.IsNullOrEmpty(PreviousValue) ? $" ({PreviousValue})" : "";
      return $"    [{(localizer != null ? localizer["FIELD"] : "FIELD")}-{(localizer != null ? localizer[Action.ToString()] : Action.ToString())}] {Name}: {ActualValue}{previousValue}";
    }

    public string GetTranslatedResultText(IStringLocalizer<TileController> localizer) => GetResultText(localizer);

    public override string ToString() => GetResultText();
  }
}