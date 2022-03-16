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

    public string GetResultText(IStringLocalizer<TileController> localizer)
    {
      string previousValue = !string.IsNullOrEmpty(PreviousValue) ? $" ({PreviousValue})" : "";
      return $"    [{localizer["FIELD"]}-{localizer[Action.ToString()]}] {Name}: {ActualValue}{previousValue}";
    }
  }
}