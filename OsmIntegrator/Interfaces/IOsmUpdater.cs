using System.Threading.Tasks;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Interfaces
{
  public interface IOsmUpdater
  {
    Task<TileImportReport> Update(DbTile tile, ApplicationDbContext dbContext, Osm osmRoot);

    Task<List<TileImportReport>> Update(List<DbTile> tiles, ApplicationDbContext dbContext, Osm osmRoot);

    Task UpdateTileReferences(List<DbTile> tiles, ApplicationDbContext dbContext);

    bool ContainsChanges(DbTile tile, Osm osmRoot);
  }
}