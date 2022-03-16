using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.CsvObjects;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Tools
{
  public class GtfsReportsFactory : IGtfsReportsFactory
  {
    public void AddField(ReportStop reportStop, string name, string actualValue, string previousValue,
  ChangeAction action)
    {
      reportStop.Fields ??= new List<ReportField>();
      reportStop.Fields.Add(new ReportField
      {
        Name = name,
        ActualValue = actualValue,
        PreviousValue = previousValue,
        Action = action
      });
    }

    public void UpdateName(ReportStop reportStop, string name)
    {
      reportStop.PreviousName = reportStop.Name;
      reportStop.Name = name;
    }

    public GtfsImportReport Create()
    {
      return new();
    }

    public ReportStop CreateStop(GtfsImportReport report, GtfsStop stop, ChangeAction action, bool reverted = false)
    {
      ReportStop reportStop;
      if (action == ChangeAction.Modified)
      {
        reportStop = new()
        {
          Name = stop.stop_name,
          Version = 0,
          StopId = stop.stop_id.ToString(),
          StopType = StopType.Gtfs,
          Action = action,
          Reverted = reverted
        };
      }
      else if (action == ChangeAction.Added)
      {
        reportStop = new()
        {
          Name = stop.stop_name,
          Version = 0,
          StopId = stop.stop_id.ToString(),
          StopType = StopType.Gtfs,
          Action = action,
          Reverted = reverted
        };
      }
      else
      {
        reportStop = new()
        {
          Name = stop.stop_name,
          StopId = stop.stop_id.ToString(),
          StopType = StopType.Gtfs,
          Action = action,
          Reverted = reverted
        };
      }

      report.Stops.Add(reportStop);
      return reportStop;
    }
  }
}