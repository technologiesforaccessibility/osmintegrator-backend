using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Globalization;
using System.IO;
using System.Linq;
using OsmIntegrator.Tools;
using System.Xml.Serialization;
using System.Text;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator;

public class Overpass : IOverpass
{
  private readonly HttpClient _httpClient;

  public Overpass(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }
  private async Task<Osm> GetContent(HttpContent content)
  {
    HttpResponseMessage result = new HttpResponseMessage();
    for (int i = 0; i < 5; i++)
    {
      result = await _httpClient
        .SendAsync(
        new HttpRequestMessage(HttpMethod.Get, Constants.OVERPASS_API_LINK)
        {
          Content = content
        }
        );

      if (result.IsSuccessStatusCode)
      {
        Stream responseStream = result.Content.ReadAsStream();

        XmlSerializer serializer = new XmlSerializer(typeof(Osm));

        return (Osm)serializer.Deserialize(responseStream);
      }
    }

    throw new HttpRequestException();
  }

  private async Task<Osm> GetContent(HttpContent content, CancellationToken cancelationToken)
  {
    HttpResponseMessage result = new HttpResponseMessage();
    for (int i = 0; i < 5; i++)
    {
      result = await _httpClient
      .SendAsync(
      new HttpRequestMessage(HttpMethod.Get, Constants.OVERPASS_API_LINK)
      {
        Content = content
      },
      cancelationToken
      );

      if (result.IsSuccessStatusCode)
      {
        Stream responseStream = result.Content.ReadAsStream();

        XmlSerializer serializer = new XmlSerializer(typeof(Osm));

        return (Osm)serializer.Deserialize(responseStream);
      }
    }

    throw new HttpRequestException();
  }

  public async Task<Osm> GetFullArea(ApplicationDbContext dbContext, CancellationToken cancellationToken)
  {
    double margin = Constants.OVERPASS_MARGIN;

    double minLat = dbContext.Stops.Min(x => x.Lat) - margin;
    double minLong = dbContext.Stops.Min(x => x.Lon) - margin;
    double maxLat = dbContext.Stops.Max(x => x.Lat) + margin;
    double maxLong = dbContext.Stops.Max(x => x.Lon) + margin;

    return await GetContent(
      new StringContent(
        $"node [{Constants.OVERPASS_DOWNLOAD_QUERY}] " + 
        $"({minLat.ToString(CultureInfo.InvariantCulture)}, " + 
        $"{minLong.ToString(CultureInfo.InvariantCulture)}, " + 
        $"{maxLat.ToString(CultureInfo.InvariantCulture)}, " + 
        $"{maxLong.ToString(CultureInfo.InvariantCulture)}); out meta;",
        Encoding.UTF8),
        cancellationToken);
  }

  public async Task<Osm> GetArea(double minLat, double minLong, double maxLat, double maxLong)
  {
    string overpassQuery =
      $"node [{Constants.OVERPASS_DOWNLOAD_QUERY}] " +
      $"({minLat.ToString(CultureInfo.InvariantCulture)}, " +
      $"{minLong.ToString(CultureInfo.InvariantCulture)}, " +
      $"{maxLat.ToString(CultureInfo.InvariantCulture)}, " +
      $"{maxLong.ToString(CultureInfo.InvariantCulture)}); out meta;";

    return await GetContent(new StringContent(overpassQuery,
          Encoding.UTF8));
  }
}