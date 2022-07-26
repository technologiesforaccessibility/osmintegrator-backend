using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using OsmIntegrator.OsmApi;

namespace OsmIntegrator.ApiModels.OsmExport
{
  public class OsmExportResult
  {
    public OsmApiResponse ApiResponse { get; }

    public uint? ChangesetId => ApiResponse?.Data as uint?;

    public OsmExportResult(OsmApiResponse apiResponse)
    {
      ApiResponse = apiResponse;
    }
  }
}