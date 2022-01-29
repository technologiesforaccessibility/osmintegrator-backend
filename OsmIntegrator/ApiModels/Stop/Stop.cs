using System;
using System.Collections.Generic;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.ApiModels.Stops
{
  public class Stop
  {
    public Guid Id { get; set; }
    public long StopId { get; set; }
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double? InitLat { get; set; }
    public double? InitLon { get; set; }
    public string Number { get; set; }
    public List<Tag> Tags { get; set; }
    public StopType StopType { get; set; }
    public ProviderType ProviderType { get; set; }
    public Guid TileId { get; set; }
    public Tile Tile { get; set; }
    public bool OutsideSelectedTile { get; set; } = false;
    public int Version { get; set; }
    public string Changeset { get; set; }

    public bool IsDeleted { get; set; } = false;
  }
}