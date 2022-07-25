using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.OsmExport;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Extensions;
using OsmIntegrator.Tools;

namespace OsmIntegrator.OsmApi
{
  public class OsmExportBuilder : IOsmExportBuilder
  {
    private readonly IOsmApiClient _osmApiClient;
    private OsmChange _osmChange;
    private OsmChangeset _osmChangeset;
    private string _username;
    private string _password;
    private Uri _osmApiUrl;
    private bool _close;

    public OsmExportBuilder(IOsmApiClient osmApiClient)
    {
      _osmApiClient = osmApiClient;
    }

    public IOsmExportBuilder UseOsmApiUrl(string osmApiUrl)
    {
      _osmApiUrl = string.IsNullOrWhiteSpace(osmApiUrl)
        ? throw new ArgumentNullException(nameof(osmApiUrl))
        : osmApiUrl.ToUri();

      return this;
    }

    public IOsmExportBuilder UseUsername(string username)
    {
      _username = string.IsNullOrWhiteSpace(username)
        ? throw new ArgumentNullException(nameof(username))
        : username;

      return this;
    }

    public IOsmExportBuilder UsePassword(string password)
    {
      _password = string.IsNullOrWhiteSpace(password)
        ? throw new ArgumentNullException(nameof(password))
        : password;

      return this;
    }

    public IOsmExportBuilder UseClose(bool close = true)
    {
      _close = close;

      return this;
    }

    public IOsmExportBuilder UseOsmChange(OsmChange osmChange)
    {
      ArgumentNullException.ThrowIfNull(osmChange);
      _osmChange = osmChange;
      return this;
    }
    
    public IOsmExportBuilder UseOsmChangeset(OsmChangeset osmChangeset)
    {
      ArgumentNullException.ThrowIfNull(osmChangeset);
      _osmChangeset = osmChangeset;
      return this;
    }

    public async Task<OsmExportResult> ExportAsync()
    {
      IOsmApiClient osmApiClient = _osmApiClient
        .WithBaseUrl(_osmApiUrl)
        .WithUsername(_username)
        .WithPassword(_password);

      try
      {
        uint changesetId = await osmApiClient.CreateChangesetAsync(_osmChangeset);

        foreach (Node node in _osmChange.Mod.Nodes)
        {
          node.Changeset = changesetId.ToString();
        }

        await osmApiClient.UploadChangesAsync(changesetId, _osmChange);

        if (_close)
        {
          await osmApiClient.CloseChangesetAsync(changesetId);
        }

        return new OsmExportResult(OsmApiResponse.Success(changesetId));
      }
      catch (UnauthorizedAccessException)
      {
        return new OsmExportResult(OsmApiResponse.Error(OsmApiStatusCode.Unauthorized));
      }
    }
  }
}