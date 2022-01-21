using System.Collections.Generic;
using OsmIntegrator.Database.Models;

public interface IOsmExporter
{
  string GetOsmChangeFile(DbTile tile);
  string GetComment(long x, long y, byte zoom);
  IReadOnlyDictionary<string, string> GetTags(string comment);
}