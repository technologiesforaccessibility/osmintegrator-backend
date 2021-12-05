using System.Threading;
using System.Threading.Tasks;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Tests.Mocks
{
  public class OverpassMock : IOverpass
  {
    public Task<Osm> GetArea(double minLat, double minLong, double maxLat, double maxLong)
    {
      throw new System.NotImplementedException();
    }

    public Task<Osm> GetFullArea(ApplicationDbContext dbContext, CancellationToken cancelationToken)
    {
      throw new System.NotImplementedException();
    }
  }
}