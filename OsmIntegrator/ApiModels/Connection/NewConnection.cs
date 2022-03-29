using System;

namespace OsmIntegrator.ApiModels.Connections
{
  public class NewConnection
  {
    public Guid ConnectionId { get; set; }
    public string Message { get; set; }
  }
}