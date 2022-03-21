using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;

namespace OsmIntegrator.Tests.Tests.GtfsImports
{
  public class ImportTestBase : IntegrationTest
  {
    public ImportTestBase(ApiWebApplicationFactory factory) : base(factory)
    {
      TestDataFolder = $"Data/GtfsImports/";
    }

    protected DbStop GetExpectedStop(long id, double? lat = null, double? lon = null, string name = null, string number = null)
    {
      DbStop stop = _dbContext.Stops.First(x => x.StopId == id);

      stop.Lat = lat ??= stop.Lat;
      stop.Lon = lon ??= stop.Lon;

      stop.Name = name ??= stop.Name;
      stop.Number = number ??= stop.Number;

      return stop;
    }
  }
}

