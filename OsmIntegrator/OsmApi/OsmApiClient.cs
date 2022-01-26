using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using OsmIntegrator.Extensions;
using OsmIntegrator.Tools;

namespace OsmIntegrator.OsmApi
{
  public class OsmApiClient : IOsmApiClient
  {
    private const string CREATE_CHANGESET = "/api/0.6/changeset/create";
    private const string UPLOAD_CHANGE = "/api/0.6/changeset/{0}/upload";
    private const string CLOSE_CHANGESET = "/api/0.6/changeset/{0}/close";

    private readonly HttpClient _httpClient;

    private Uri _baseUrl;
    private string _username;
    private string _password;

    public OsmApiClient(HttpClient httpClient)
    {
      _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public IOsmApiClient WithBaseUrl(string baseUrl)
    {
      if (string.IsNullOrWhiteSpace(baseUrl))
      {
        throw new ArgumentNullException(nameof(baseUrl));
      }

      return WithBaseUrl(baseUrl.ToUri());
    }

    public IOsmApiClient WithBaseUrl(Uri baseUrl)
    {
      _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));

      return this;
    }

    public IOsmApiClient WithUsername(string username)
    {
      _username = username ?? throw new ArgumentNullException(nameof(username));

      return this;
    }

    public IOsmApiClient WithPassword(string password)
    {
      _password = password ?? throw new ArgumentNullException(nameof(password));

      return this;
    }

    public async Task<uint> CreateChangesetAsync(OsmChangeset changeset)
    {
      if (changeset == null)
      {
        throw new ArgumentNullException(nameof(changeset));
      }

      StringContent httpBodyContent = new(changeset.ToXml(), Encoding.UTF8, "text/xml");
      Uri createChangesetUrl = _baseUrl.UseRelativePath(CREATE_CHANGESET);

      string responseBody = await SendAuthRequest(HttpMethod.Put, createChangesetUrl, httpBodyContent);

      return uint.Parse(responseBody);
    }

    public async Task CloseChangesetAsync(uint changesetId)
    {
      StringContent emptyHttpBodyContent = new(string.Empty, Encoding.UTF8);
      Uri closeChangesetUrl = new Uri(_baseUrl, string.Format(CLOSE_CHANGESET, changesetId.ToString()));

      await SendAuthRequest(HttpMethod.Put, closeChangesetUrl, emptyHttpBodyContent);
    }

    public async Task UploadChangesAsync(uint changesetId, OsmChange change)
    {
      if (change == null)
      {
        throw new ArgumentNullException(nameof(change));
      }

      StringContent httpBodyContent = new(change.ToXml(), Encoding.UTF8, "text/xml");
      Uri uploadToChangesetUrl = _baseUrl.UseRelativePath(UPLOAD_CHANGE, changesetId);

      await SendAuthRequest(HttpMethod.Post, uploadToChangesetUrl, httpBodyContent);
    }

    private async Task<string> SendAuthRequest(HttpMethod method, Uri address, HttpContent requestContent = null)
    {
      using (HttpRequestMessage request = new HttpRequestMessage(method, address))
      {
        AddAuthentication(request);
        request.Content = requestContent;
        HttpResponseMessage response = await _httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
          throw new Exception(response.ReasonPhrase);
        }

        return await response.Content.ReadAsStringAsync();
      }
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
      var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_username + ":" + _password));
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
    }
  }
}