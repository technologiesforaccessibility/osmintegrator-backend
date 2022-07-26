using System.Collections.Generic;
using System.Linq;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Extensions
{
  public static class DbConnectionsExtensions
  {
    public static IEnumerable<DbConnection> OnlyLatest(this IEnumerable<DbConnection> connections) =>
      connections
        .GroupBy(c => new { c.GtfsStopId, c.OsmStopId })
        .Select(g => g.OrderByDescending(c => c.CreatedAt).FirstOrDefault());

    public static IEnumerable<DbConnection> OnlyActive(this IEnumerable<DbConnection> connections) =>
      connections
        .OnlyLatest()
        .Where(c => c.OperationType == ConnectionOperationType.Added && !c.Exported);

    public static IEnumerable<DbConnection> OnlyExported(this IEnumerable<DbConnection> connections) =>
      connections.OnlyLatest()
        .Where(c => c.OperationType == ConnectionOperationType.Added && c.Exported);
  }
}