using System.Text;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ReportField
  {
    public string Name { get; set; }
    public string ActualValue { get; set; }
    public string PreviousValue { get; set; }
    public ChangeAction Action { get; set; }

    public override string ToString()
    {
      string previousValue = !string.IsNullOrEmpty(PreviousValue) ? $", ({PreviousValue})" : "";
      return $"        [FIELD-{Action.ToString()}] {Name}: {ActualValue}{previousValue}";
    }
  }
}