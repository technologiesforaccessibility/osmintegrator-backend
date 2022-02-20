using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Interfaces
{
  public interface IReportsFactory
  {
    void UpdateName(ReportStop reportStop, string name);
    TileImportReport Create(DbTile tile);
    ReportStop CreateStop(TileImportReport report, Node node, DbStop stop, ChangeAction action, bool reverted = false);
    void AddField(ReportStop reportStop, string name, string actualValue, string previousValue, ChangeAction action);
  }
}