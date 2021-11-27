using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Interfaces
{
  public interface IOsmRefresherHelper
  {
    Task Refresh(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot);

    Task Refresh(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot);

  }
}