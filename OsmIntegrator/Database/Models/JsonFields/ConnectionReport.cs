using System;
using System.Collections.Generic;
using System.Text;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.Database.Models.JsonFields
{
  public class ConnectionReport
  {
    public Guid GtfsStopId { get; set; }
    public string GtfsStopName { get; set; }
    public Guid OsmStopId { get; set; }
    public string OsmStopName { get; set; }
    public ChangeAction Action { get; set; }
  }
}