using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.OsmExport;
using OsmIntegrator.Tools;

namespace OsmIntegrator.OsmApi
{
  public interface IOsmExportBuilder
  {
    IOsmExportBuilder UseOsmApiUrl(string osmApiUrl);
    IOsmExportBuilder UseOsmChange(OsmChange osmChange);
    IOsmExportBuilder UseOsmChangeset(OsmChangeset osmChangeset);
    IOsmExportBuilder UsePassword(string password);
    IOsmExportBuilder UseUsername(string username);
    IOsmExportBuilder UseClose(bool close = true);
    Task<OsmExportResult> ExportAsync();
  }
}