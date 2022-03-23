using CsvHelper.Configuration.Attributes;

namespace OsmIntegrator.Database.Models.CsvObjects
{
  public class GtfsStop
  {
    [Name("stop_id")]
    public int StopId { get; set; }

    [Name("stop_code")]
    public string StopCode { get; set; }

    [Name("stop_name")]
    public string StopName { get; set; }

    [Name("stop_desc")]
    public string StopDesc { get; set; }

    [Name("stop_lat")]
    public string StopLat { get; set; }

    [Name("stop_lon")]
    public string StopLon { get; set; }

    [Name("stop_url")]
    public string StopUrl { get; set; }

    [Name("location_type")]
    public int LocationType { get; set; }
  }
}