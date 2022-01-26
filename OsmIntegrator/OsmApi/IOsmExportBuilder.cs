using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OsmIntegrator.OsmApi
{
  public interface IOsmExportBuilder
  {
    IOsmExportBuilder UseOsmApiUrl(string osmApiUrl);
    IOsmExportBuilder UsePassword(string password);
    IOsmExportBuilder UseUsername(string username);
    IOsmExportBuilder UseTile(Guid tileId);
    IOsmExportBuilder UseChangesetComment(string comment);
    IOsmExportBuilder UseClose(bool close = true);

    Task<uint> ExportAsync();
  }
}