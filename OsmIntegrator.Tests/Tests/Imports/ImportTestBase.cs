using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Mocks;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Imports
{
  public class ImportTestBase : IntegrationTest
  {
    protected const double EXPECTED_LAT_1 = 50.2313803;
    protected const double EXPECTED_LON_2 = 18.9893557;
    protected const double EXPECTED_LAT_3 = 50.2326754;
    protected const double EXPECTED_LON_3 = 18.9956495;
    
    public ImportTestBase(ApiWebApplicationFactory factory) : base(factory)
    {
      TestDataFolder = $"Data/Imports/";
    }

    protected async Task<Report> UpdateTileAsync(string tileId)
    {
      var response = await _client.PutAsync($"/api/Tile/UpdateStops/{tileId}", null);
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Report>(jsonResponse);
    }

    protected async Task<bool> ContainsChanges(string tileId)
    {
      var response = await _client.GetAsync($"/api/Tile/ContainsChanges/{tileId}");
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return bool.Parse(jsonResponse);
    }

    protected DbStop GetExpectedStop(long id, double? lat = null, double? lon = null)
    {
      DbStop stop = _dbContext.Stops.First(x => x.StopId == id);

      stop.Lat = lat ??= stop.Lat;
      stop.Lon = lon ??= stop.Lon;
      stop.Version++;

      return stop;
    }
  }
}

