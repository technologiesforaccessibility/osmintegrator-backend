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
    private readonly IOsmExporter _osmExporter;
    private string _username;
    private string _password;
    private string _changesetComment;
    private Uri _osmApiUrl;
    private bool _close = false;
    private Guid? _tileId;

    public OsmExportBuilder(IOsmApiClient osmApiClient, IOsmExporter osmExporter)
    {
      _osmApiClient = osmApiClient;
      _osmExporter = osmExporter;
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

    public IOsmExportBuilder UseChangesetComment(string comment)
    {
      _changesetComment = string.IsNullOrWhiteSpace(comment)
        ? throw new ArgumentNullException(nameof(comment))
        : comment;


      return this;
    }

    public IOsmExportBuilder UseTile(Guid tileId)
    {
      _tileId = tileId;

      return this;
    }

    public IOsmExportBuilder UseClose(bool close = true)
    {
      _close = close;

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
        IReadOnlyCollection<DbConnection> connections = await _osmExporter.GetUnexportedOsmConnectionsAsync(_tileId.Value);

        OsmChangeset osmChangeset = _osmExporter.CreateChangeset(_changesetComment);

        uint changesetId = await osmApiClient.CreateChangesetAsync(osmChangeset);

        OsmChange osmChange = _osmExporter.GetOsmChange(connections, changesetId);

        await osmApiClient.UploadChangesAsync(changesetId, osmChange);

        if (_close)
        {
          await osmApiClient.CloseChangesetAsync(changesetId);
        }

        return new OsmExportResult(OsmApiResponse.Success(changesetId), connections);
      }
      catch (UnauthorizedAccessException)
      {
        return new OsmExportResult(OsmApiResponse.Error(OsmApiStatusCode.Unauthorized));
      }
    }
  }
}