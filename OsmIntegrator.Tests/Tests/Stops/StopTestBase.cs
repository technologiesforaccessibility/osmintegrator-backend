using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Tests.Fixtures;

namespace OsmIntegrator.Tests.Tests.Stops;

public class StopsTestBase : IntegrationTest
{
  public StopsTestBase(ApiWebApplicationFactory factory) : base(factory)
  {
    TestDataFolder = "Data/";
  }

  protected async Task<Stop> ChangePosition(StopPositionData stopPositionData)
  {
    var json = JsonConvert.SerializeObject(stopPositionData);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _client.PutAsync($"/api/Stop/ChangePosition", content);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    string jsonResponse = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<Stop>(jsonResponse);
  }

  protected async Task<Stop> ResetPosition(string stopId)
  {
    var response = await _client.PostAsync($"/api/Stop/ResetPosition/{stopId}", null);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    string jsonResponse = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<Stop>(jsonResponse);
  }

  protected async Task<List<Stop>> GetAllStops()
  {
    HttpResponseMessage response = await _client.GetAsync("/api/Stop");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string jsonResponse = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<Stop[]>(jsonResponse).ToList();
  }
}