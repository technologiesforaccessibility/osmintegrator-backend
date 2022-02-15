using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.QueryObjects;

namespace OsmIntegrator.Extensions;

public static class DbContextExtensions
{
  public static async Task<List<ConnectionQuery>> GetAllConnectionsWithUserName(this ApplicationDbContext dbContext) =>
    await dbContext.Connections
      .AsNoTracking()
      .Select(c => new ConnectionQuery
      {
        GtfsStopId = c.GtfsStopId,
        OsmStopId = c.OsmStopId,
        CreatedAt = c.CreatedAt,
        OperationType = c.OperationType,
        TileId = c.GtfsStop.TileId,
        UserName = c.User.UserName,
        UserId = c.UserId,
        Exported = c.Exported
      })
      .ToListAsync();

  public static async Task<List<UncommittedTileQuery>> GetUncommittedTilesQuery(this ApplicationDbContext dbContext,
    Dictionary<Guid, List<ConnectionQuery>> activeConnectionsTileGroup) =>
    await dbContext.Tiles
      .AsNoTracking()
      .Where(t => activeConnectionsTileGroup.Keys.Contains(t.Id))
      .Select(t => new UncommittedTileQuery
      {
        Id = t.Id,
        MaxLat = t.MaxLat,
        MaxLon = t.MaxLon,
        MinLat = t.MinLat,
        MinLon = t.MinLon,
        X = t.X,
        Y = t.Y,
        GtfsStopsCount = t.Stops.Count(s => s.StopType == StopType.Gtfs)
      })
      .ToListAsync();

  public static async Task<List<Tile>> GetCurrentUserTiles(this ApplicationDbContext dbContext,
    HashSet<Guid> unavailableTiles) =>
    await dbContext.Tiles
      .AsNoTracking()
      .Include(x => x.Stops)
      .Where(t => t.Stops.Any(x => x.StopType == StopType.Gtfs))
      .Where(t => !unavailableTiles.Contains(t.Id))
      .Select(t => new Tile
      {
        Id = t.Id,
        MaxLat = t.MaxLat,
        MaxLon = t.MaxLon,
        MinLat = t.MinLat,
        MinLon = t.MinLon,
        X = t.X,
        Y = t.Y,
        GtfsStopsCount = t.Stops.Count(s => s.StopType == StopType.Gtfs)
      })
      .ToListAsync();
}