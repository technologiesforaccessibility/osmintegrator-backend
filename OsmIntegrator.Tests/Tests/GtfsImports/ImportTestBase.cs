using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;

namespace OsmIntegrator.Tests.Tests.GtfsImports
{
  public class ImportTestBase : IntegrationTest
  {
    protected const double EXPECTED_LAT_1 = 50.2313803;
    protected const double EXPECTED_LON_2 = 18.9893557;
    protected const double EXPECTED_LAT_3 = 50.2326754;
    protected const double EXPECTED_LON_3 = 18.9956495;

    public ImportTestBase(ApiWebApplicationFactory factory) : base(factory)
    {
      TestDataFolder = $"Data/GtfsImports/";
    }

    protected DbStop GetExpectedStop(long id, double? lat = null, double? lon = null)
    {
      DbStop stop = _dbContext.Stops.First(x => x.StopId == id);

      stop.Lat = lat ??= stop.Lat;
      stop.Lon = lon ??= stop.Lon;

      return stop;
    }
  }
}

