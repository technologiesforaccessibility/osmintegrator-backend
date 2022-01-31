using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;

public interface IOsmExporter
{
  Task<IReadOnlyCollection<DbConnection>> GetUnexportedOsmConnectionsAsync(Guid tileId);
  OsmChange GetOsmChange(IReadOnlyCollection<DbConnection> connections, uint? changesetId = null);
  string GetComment(long x, long y, byte zoom);
  IReadOnlyDictionary<string, string> GetTags(string comment);
  OsmChangeset CreateChangeset(string comment);
}