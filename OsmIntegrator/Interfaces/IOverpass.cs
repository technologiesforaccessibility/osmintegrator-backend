using Microsoft.EntityFrameworkCore;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;
using System.Threading;
using System.Threading.Tasks;

namespace OsmIntegrator.Interfaces
{

  public interface IOverpass
  {
    Task<Osm> GetArea(double minLat, double minLong, double maxLat, double maxLong);

    Task<Osm> GetFullArea(ApplicationDbContext dbContext, CancellationToken cancelationToken);
  }
}