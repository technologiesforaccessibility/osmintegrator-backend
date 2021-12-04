using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Interfaces
{
  public interface IReportsFactory
  {
    void UpdateName(ReportStop reportStop, string name);
    TileReport Create(DbTile tile);
    ReportStop CreateStop(TileReport report, Node node, DbStop stop, ChangeAction action);
    void AddField(ReportStop reportStop, string name, string actualValue, string previousValue, ChangeAction action);
  }
}