using System;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.Extensions;

public static class DbTilesExtensions
{
  public static IQueryable<DbTile> OnlyAccessibleBy(this IQueryable<DbTile> tiles, Guid userId) =>
    tiles.Where(x => !x.Stops.Where(
      s => s.StopType == StopType.Gtfs).Any(
        s => s.GtfsConnections.Any(c => c.UserId != userId)));
}
