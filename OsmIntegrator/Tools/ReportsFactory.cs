using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Tools
{
  public class ReportsFactory : IReportsFactory
  {
    public TileImportReport Create(DbTile tile)
    {
      return new()
      {
        TileId = tile.Id,
        TileX = tile.X,
        TileY = tile.Y,
      };
    }

    public ReportStop CreateStop(TileImportReport report, Node node, DbStop stop, ChangeAction action, bool reverted)
    {
      ReportStop reportStop;
      if (action == ChangeAction.Modified)
      {
        reportStop = new()
        {
          Name = stop.Name,
          Version = node.Version,
          PreviousVersion = stop.Version == node.Version ? null : stop.Version,
          Changeset = node.Changeset,
          PreviousChangeset = stop.Changeset,
          StopId = node.Id,
          DatabaseStopId = stop.Id,
          StopType = StopType.Osm,
          Action = action,
          Reverted = reverted
        };
      }
      else if (action == ChangeAction.Added)
      {
        reportStop = new()
        {
          Version = node.Version,
          Changeset = node.Changeset,
          StopId = node.Id,
          DatabaseStopId = stop.Id,
          StopType = StopType.Osm,
          Action = action,
          Reverted = reverted
        };
      }
      else
      {
        reportStop = new()
        {
          Name = stop.Name,
          Version = stop.Version,
          Changeset = stop.Changeset,
          StopId = stop.StopId.ToString(),
          DatabaseStopId = stop.Id,
          StopType = StopType.Osm,
          Action = action,
          Reverted = reverted
        };
      }

      report.Stops.Add(reportStop);
      return reportStop;
    }
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
  }
}