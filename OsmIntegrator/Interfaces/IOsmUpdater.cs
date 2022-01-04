using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Interfaces
{
  public interface IOsmUpdater
  {
    Task<ReportTile> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot);

    Task<List<ReportTile>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot);

    bool ContainsChanges(DbTile tile, Osm osmRoot);
  }
}