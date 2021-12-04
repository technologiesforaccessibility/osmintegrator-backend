using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Interfaces
{
  public interface IOsmUpdater
  {
    Task Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot);

    Task Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot);

  }
}