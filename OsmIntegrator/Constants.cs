namespace OsmIntegrator;

public class Constants
{
  // Osm tags
  public const string LOCAL_REF = "local_ref";
  public const string REF = "ref";
  public const string REF_METROPOLIA = "ref:metropoliaztm";
  public const string NAME = "name";

  // Overpass
  public const string OVERPASS_API_LINK = "https://lz4.overpass-api.de/api/interpreter";
  public const string OVERPASS_DOWNLOAD_QUERY = "~'highway|railway'~'tram_stop|bus_stop'";
  public const double OVERPASS_MARGIN = 0.001;

  public const string OSM_INTEGRATOR_NAME = "osm integrator v0.1";
  public const string OSM_API_VERSION = "0.6";
  
  public const string IMPORT_WIKI_ADDRESS =
    "Wiki: https://wiki.openstreetmap.org/w/index.php?title=Automated_edits/luktar/OsmIntegrator_-_fixing_stop_signs_for_blind";

}