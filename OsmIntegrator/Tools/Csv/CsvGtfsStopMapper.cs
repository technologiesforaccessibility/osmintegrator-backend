using CsvHelper.Configuration;

namespace OsmIntegrator.Tools.Csv;

public class CsvGtfsStopMapper : ClassMap<CsvGtfsStop>
{
  public CsvGtfsStopMapper()
  {
    Map(p => p.StopId).Name("stop_id");
    Map(p => p.StopCode).Name("stop_code");
    Map(p => p.StopName).Name("stop_name");
    Map(p => p.StopDesc).Name("stop_desc");
    Map(p => p.StopLat).Name("stop_lat");
    Map(p => p.StopLon).Name("stop_lon");
    Map(p => p.StopUrl).Name("stop_url");
    Map(p => p.LocationType).Name("location_type");
  }
}