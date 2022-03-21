using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.OsmImports
{
  public class PositionTest : ImportTestBase
  {
    public PositionTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(PositionTest), "supervisor2", "supervisor1");

      DbStop expectedStop1 = GetExpectedStop(ExpectedValues.OSM_STOP_ID_1, EXPECTED_LAT_1);
      DbStop expectedStop2 = GetExpectedStop(ExpectedValues.OSM_STOP_ID_2, null, EXPECTED_LON_2);
      DbStop expectedStop3 = GetExpectedStop(ExpectedValues.OSM_STOP_ID_3, EXPECTED_LAT_3, EXPECTED_LON_3);

      DbTile tile = _dbContext.Tiles.AsNoTracking().First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(PositionTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_1);
      Assert.Equal(expectedStop1.Lat, actualStop1.Lat);
      Assert.Equal(expectedStop1.Version, actualStop1.Version);

      DbStop actualStop2 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_2);
      Assert.Equal(expectedStop2.Lon, actualStop2.Lon);
      Assert.Equal(expectedStop2.Version, actualStop2.Version);

      DbStop actualStop3 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_3);
      Assert.Equal(expectedStop3.Lat, actualStop3.Lat);
      Assert.Equal(expectedStop3.Lon, actualStop3.Lon);
      Assert.Equal(expectedStop3.Version, actualStop3.Version);

      TileImportReport actualReportTile =
        _dbContext.ChangeReports.AsNoTracking().FirstOrDefault(x => x.TileId == tile.Id)?.TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(PositionTest)}/ReportTile.json");

      Assert.Empty(Compare<TileImportReport>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}