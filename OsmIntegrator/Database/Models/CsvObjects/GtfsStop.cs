
namespace OsmIntegrator.Database.Models.CsvObjects
{
  public class GtfsStop
  {
    public int stop_id { get; set; }
    public string stop_code { get; set; }
    public string stop_name { get; set; }
    public string stop_desc { get; set; }
    public double stop_lat { get; set; }
    public double stop_lon { get; set; }
    public string stop_url { get; set; }
    public int location_type { get; set; }
  }
}