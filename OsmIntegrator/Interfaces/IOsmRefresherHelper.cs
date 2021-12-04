using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;
using OsmIntegrator.ApiModels.Reports;

namespace OsmIntegrator.Interfaces
{
  public interface IOsmUpdater
  {
    Task<TileReport> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot);

    Task<List<TileReport>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot);

  }
}