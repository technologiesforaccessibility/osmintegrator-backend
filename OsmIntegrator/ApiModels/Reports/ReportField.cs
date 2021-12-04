namespace OsmIntegrator.ApiModels.Reports
{
  public class ReportField
  {
    public string Name { get; set; }
    public string ActualValue { get; set; }
    public string PreviousValue { get; set; }
    public ChangeAction Action { get; set; }
  }
}