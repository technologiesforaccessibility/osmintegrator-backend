namespace OsmIntegrator.Tools.Csv
{
  public class CsvGtfsStop
  {
    public int StopId { get; set; }
    public string StopCode { get; set; }
    public string StopName { get; set; }
    public string StopDesc { get; set; }
    public string StopLat { get; set; }
    public string StopLon { get; set; }
    public string StopUrl { get; set; }
    public int LocationType { get; set; }
  }
}