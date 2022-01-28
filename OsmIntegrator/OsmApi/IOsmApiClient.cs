using System;
using System.Threading.Tasks;
using OsmIntegrator.Tools;

namespace OsmIntegrator.OsmApi
{
  public interface IOsmApiClient
  {
    Task CloseChangesetAsync(uint changesetId);
    Task<uint> CreateChangesetAsync(OsmChangeset changeset);
    Task UploadChangesAsync(uint changesetId, OsmChange change);
    IOsmApiClient WithPassword(string password);
    IOsmApiClient WithUsername(string username);
    IOsmApiClient WithBaseUrl(string baseUrl);
    IOsmApiClient WithBaseUrl(Uri baseUrl);
  }
}