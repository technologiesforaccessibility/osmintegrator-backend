using System;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.ApiModels.Reports
{
  public class ReportStop
  {
    public string Name { get; set; }
    public string PreviousName { get; set; }
    public int Version { get; set; }
    public int? PreviousVersion { get; set; }
    public string Changeset { get; set; }
    public string PreviousChangeset { get; set; }
    public string StopId { get; set; }
    public StopType StopType { get; set; }
    public Guid? DatabaseStopId { get; set; }
    public List<ReportField> Fields { get; set; }
    public ChangeAction Action { get; set; }
  }
}