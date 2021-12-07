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
    private const int RIGHT_TILE_X = 2264;
    private const int RIGHT_TILE_Y = 1385;

    private const long STOP_ID_1 = 1831944331;
    private const long STOP_ID_2 = 1905039171;
    private const long STOP_ID_3 = 1584594015;

    private const double EXPECTED_LAT_1 = 50.2313803;
    private const double EXPECTED_LON_2 = 18.9893557;
    private const double EXPECTED_LAT_3 = 50.2326754;
    private const double EXPECTED_LON_3 = 18.9956495;
    private static readonly string OSM_UPDATE_FOLDER = $"Data/{nameof(OsmUpdateTest)}/";

    private readonly IOverpass _overpass;
    private readonly OverpassMock _overpassMock;

    private LoginData _defaultLoginData = new LoginData
    {
      Email = "supervisor2@abcd.pl",
      Password = "supervisor2#12345678",
    };
    public OsmUpdateTest(ApiWebApplicationFactory fixture) : base(fixture)
    {
      _overpass = _factory.Services.GetService<IOverpass>();
      _overpassMock = (OverpassMock)_overpass;
    }

    private void InitializeDb(string testName)
    {
      List<DbStop> osmStops =
        _dataInitializer.GetOsmStopsList($"{OSM_UPDATE_FOLDER}{testName}/OsmStopsInit.xml").ToList();

      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      _dataInitializer.InitializeStopsAndTiles(_dbContext, null, osmStops);
      transaction.Commit();
    }

    public async Task<Report> UpdateTileAsync(string tileId)
    {
      var response = await _client.PutAsync($"/api/Tile/UpdateStops/{tileId}", null);
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Report>(jsonResponse);
    }

    private DbStop GetExpectedStop(long id, double? lat = null, double? lon = null)
    {
      DbStop stop = _dbContext.Stops.First(x => x.StopId == id);

      stop.Lat = lat ??= stop.Lat;
      stop.Lon = lon ??= stop.Lon;
      stop.Version++;

      return stop;
    }

    private async Task InitTest(string testName)
    {
      InitializeDb(testName);

      await LoginAndAssignTokenAsync(_defaultLoginData);
      await AssignUsersToAllTiles("supervisor2", "supervisor1");

      _overpassMock.OsmFileName = $"{OSM_UPDATE_FOLDER}{testName}/OsmStopsNew.xml";
    }

    [Fact]
    public async Task AddStopTest()
    {

    }

    [Fact]
    public async Task RemoveStopTest()
    {

    }

    [Fact]
    public async Task TagsTest()
    {
      await InitTest(nameof(TagsTest));


    }

    [Fact]
    public async Task PositionTest()
    {
      await InitTest(nameof(PositionTest));

      DbStop expectedStop1 = GetExpectedStop(STOP_ID_1, EXPECTED_LAT_1);
      DbStop expectedStop2 = GetExpectedStop(STOP_ID_2, null, EXPECTED_LON_2);
      DbStop expectedStop3 = GetExpectedStop(STOP_ID_3, EXPECTED_LAT_3, EXPECTED_LON_3);

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      Report report = await UpdateTileAsync(tile.Id.ToString());
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{OSM_UPDATE_FOLDER}{nameof(PositionTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      RefreshDb();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == STOP_ID_1);
      Assert.Equal(expectedStop1.Lat, actualStop1.Lat);
      Assert.Equal(expectedStop1.Version, actualStop1.Version);

      DbStop actualStop2 = _dbContext.Stops.First(x => x.StopId == STOP_ID_2);
      Assert.Equal(expectedStop2.Lon, actualStop2.Lon);
      Assert.Equal(expectedStop2.Version, actualStop2.Version);

      DbStop actualStop3 = _dbContext.Stops.First(x => x.StopId == STOP_ID_3);
      Assert.Equal(expectedStop3.Lat, actualStop3.Lat);
      Assert.Equal(expectedStop3.Lon, actualStop3.Lon);
      Assert.Equal(expectedStop3.Version, actualStop3.Version);

      ReportTile actualReportTile =
        _dbContext.ChangeReports.FirstOrDefault(x => x.TileId == tile.Id).TileReport;

      ReportTile expectedReportTile =
        Deserialize<ReportTile>($"{OSM_UPDATE_FOLDER}{nameof(PositionTest)}/ReportTile.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}

