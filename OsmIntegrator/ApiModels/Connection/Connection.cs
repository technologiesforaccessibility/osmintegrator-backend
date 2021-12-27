using System;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.ApiModels.Connections
{
  public class Connection
  {
    public Guid Id { get; set; }
    public Guid GtfsStopId { get; set; }

    public Guid OsmStopId { get; set; }
    public Stop OsmStop { get; set; }
    public Stop GtfsStop { get; set; }

    public bool Imported { get; set; }

    public bool Approved { get; set; }
  }
}