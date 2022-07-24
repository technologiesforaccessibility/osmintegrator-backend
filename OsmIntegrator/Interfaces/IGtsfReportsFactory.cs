using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Interfaces
{
  public interface IGtfsReportsFactory
  {
    void UpdateName(ReportStop reportStop, string name);
    GtfsImportReport Create();
    ReportStop CreateStop(GtfsImportReport report, DbStop stop, ChangeAction action, bool reverted = false);
    void AddField(ReportStop reportStop, string name, string actualValue, string previousValue, ChangeAction action);
  }
}