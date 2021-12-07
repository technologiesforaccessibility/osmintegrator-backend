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
  public class OsmUpdateTest : IntegrationTest
  {
    protected const int RIGHT_TILE_X = 2264;
    protected const int RIGHT_TILE_Y = 1385;

    protected const long STOP_ID_1 = 1831944331;
    protected const long STOP_ID_2 = 1905039171;
    protected const long STOP_ID_3 = 1584594015;

    protected const double EXPECTED_LAT_1 = 50.2313803;
    protected const double EXPECTED_LON_2 = 18.9893557;
    protected const double EXPECTED_LAT_3 = 50.2326754;
    protected const double EXPECTED_LON_3 = 18.9956495;
    protected static readonly string OSM_UPDATE_FOLDER = $"Data/{nameof(OsmUpdateTest)}/";

    protected readonly IOverpass _overpass;
    protected readonly OverpassMock _overpassMock;

    protected LoginData _defaultLoginData = new LoginData
    {
      Email = "supervisor2@abcd.pl",
      Password = "supervisor2#12345678",
    };
    public OsmUpdateTest(ApiWebApplicationFactory fixture) : base(fixture)
    {
      _overpass = _factory.Services.GetService<IOverpass>();
      _overpassMock = (OverpassMock)_overpass;
    }

    protected void InitializeDb(string testName)
    {
      List<DbStop> osmStops =
        _dataInitializer.GetOsmStopsList($"{OSM_UPDATE_FOLDER}{testName}/OsmStopsInit.xml").ToList();

      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      _dataInitializer.InitializeStopsAndTiles(_dbContext, null, osmStops);
      transaction.Commit();
    }

    protected async Task<Report> UpdateTileAsync(string tileId)
    {
      var response = await _client.PutAsync($"/api/Tile/UpdateStops/{tileId}", null);
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Report>(jsonResponse);
    }

    protected DbStop GetExpectedStop(long id, double? lat = null, double? lon = null)
    {
      DbStop stop = _dbContext.Stops.First(x => x.StopId == id);

      stop.Lat = lat ??= stop.Lat;
      stop.Lon = lon ??= stop.Lon;
      stop.Version++;

      return stop;
    }

    protected async Task InitTest(string testName)
    {
      InitializeDb(testName);

      await LoginAndAssignTokenAsync(_defaultLoginData);
      await AssignUsersToAllTiles("supervisor2", "supervisor1");

      _overpassMock.OsmFileName = $"{OSM_UPDATE_FOLDER}{testName}/OsmStopsNew.xml";
    }

    // [Fact]
    // public async Task AddStopTest()
    // {

    // }

    // [Fact]
    // public async Task RemoveStopTest()
    // {

    // }
  }
}

