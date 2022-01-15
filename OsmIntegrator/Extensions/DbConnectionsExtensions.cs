using System.Collections.Generic;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Enums;

public static class DbConnectionsExtensions
{
  public static IEnumerable<DbConnection> OnlyActive(this IEnumerable<DbConnection> connections) =>
    connections
      .GroupBy(c => new { c.GtfsStopId, c.OsmStopId })
      .Select(g => g.OrderByDescending(c => c.CreatedAt).FirstOrDefault())
      .Where(c => c.OperationType == ConnectionOperationType.Added);
}