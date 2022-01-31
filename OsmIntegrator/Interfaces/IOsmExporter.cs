using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tools;

public interface IOsmExporter
{
  Task<OsmChange> GetOsmChangeAsync(Guid tileId, uint changesetId);
  string GetComment(long x, long y, byte zoom);
  IReadOnlyDictionary<string, string> GetTags(string comment);
  OsmChangeset CreateChangeset(string comment);
}