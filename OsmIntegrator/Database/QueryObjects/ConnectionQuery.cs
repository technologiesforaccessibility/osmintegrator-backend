using System;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.QueryObjects;

public class ConnectionQuery
{
  public Guid OsmStopId { get; set; }
  public Guid GtfsStopId { get; set; }
  public Guid TileId { get; set; }
  public string UserName { get; set; }
  public Guid UserId { get; set; }
  public ConnectionOperationType OperationType { get; set; }
  public DateTime CreatedAt { get; set; }
  public bool Exported { get; set; }
}