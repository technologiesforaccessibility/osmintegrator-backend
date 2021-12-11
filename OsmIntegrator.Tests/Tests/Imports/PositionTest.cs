using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Imports
{
  public class PositionTest : ImportTestBase
  {
    public PositionTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
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

      TurnOffDbTracking();

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