using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Database.Models.CsvObjects;
using System.Threading.Tasks;
using OsmIntegrator.Database;
using OsmIntegrator.Tools;
using OsmIntegrator.Database.Models;


namespace OsmIntegrator.Interfaces
{
  public interface IGtfsUpdater
  {
    Task<GtfsImportReport> Update(GtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext, Osm osmRoot);
    bool ContainsChanges(GtfsStop[] stops, DbTile[] tiles, Osm osmRoot);
  }
}