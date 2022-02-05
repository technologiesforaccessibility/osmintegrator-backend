using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.QueryObjects;

namespace OsmIntegrator.Extensions;

public static class DbContextExtensions
{
  public async static Task<List<ConnectionQuery>> GetAllConnectionsWithUserName(this ApplicationDbContext dbContext) =>
    await dbContext.Connections
      .AsNoTracking()
      .Select(c => new ConnectionQuery
      {
        GtfsStopId = c.GtfsStopId,
        OsmStopId = c.OsmStopId,
        CreatedAt = c.CreatedAt,
        OperationType = c.OperationType,
        TileId = c.OsmStop.TileId,
        UserName = c.User == null ? null : c.User.UserName
      })
      .ToListAsync();

  public async static Task<List<TileQuery>> GetTileQuery(this ApplicationDbContext dbContext,
    Dictionary<Guid, List<ConnectionQuery>> activeConnectionsTileGroup) =>
    await dbContext.Tiles
      .AsNoTracking()
      .Where(t => activeConnectionsTileGroup.Keys.Contains(t.Id))
      .Select(t => new TileQuery
      {
        Id = t.Id,
        MaxLat = t.MaxLat,
        MaxLon = t.MaxLon,
        MinLat = t.MinLat,
        MinLon = t.MinLon,
        X = t.X,
        Y = t.Y,
        GtfsStopsCount = t.Stops.Where(s => s.StopType == StopType.Gtfs).Count()
      })
      .ToListAsync();
}