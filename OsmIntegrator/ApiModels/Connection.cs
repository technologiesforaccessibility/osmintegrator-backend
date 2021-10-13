using System;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.ApiModels
{
  public class Connection
  {
    public Guid Id { get; set; }
    public Guid GtfsStopId { get; set; }

    public Guid OsmStopId { get; set; }
    public Stop OsmStop { get; set; }
    public Stop GtfsStop { get; set; }

    public bool Imported { get; set; }

    public bool Approved => ApprovedById != null;

    public Guid? ApprovedById { get; set; }
  }
}