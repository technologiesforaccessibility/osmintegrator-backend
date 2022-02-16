namespace OsmIntegrator;

public class Constants
{
  // Osm tags
  public const string LOCAL_REF = "local_ref";
  public const string REF = "ref";
  public const string NAME = "name";

  // Overpass
  public const string OVERPASS_API_LINK = "https://lz4.overpass-api.de/api/interpreter";
  public const string OVERPASS_DOWNLOAD_QUERY = "~'highway|railway'~'tram_stop|bus_stop'";
  public const double OVERPASS_MARGIN = 0.001;
}