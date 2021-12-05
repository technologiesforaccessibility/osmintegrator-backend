using System;
using System.Collections.Generic;
using System.Text;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.Database.Models.JsonFields
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

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      string version = Version.ToString();
      if(PreviousVersion is null)
      {
        
      }
      sb.AppendLine($"\tName: {Name} ");

      return base.ToString();
    }
  }
}