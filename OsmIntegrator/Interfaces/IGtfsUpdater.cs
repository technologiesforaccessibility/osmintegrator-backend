using OsmIntegrator.Database.Models.JsonFields;
using System.Threading.Tasks;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools.Csv;


namespace OsmIntegrator.Interfaces
{
  public interface IGtfsUpdater
  {
    Task<GtfsImportReport> Update(CsvGtfsStop[] stops, DbTile[] tiles, ApplicationDbContext dbContext);
  }
}