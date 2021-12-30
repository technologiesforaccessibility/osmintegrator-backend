
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;

namespace OsmIntegrator.Tests.Tests.OsmImports
{
  public class RemoveStopTest : ImportTestBase
  {
    public RemoveStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RemoveSingleStopTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RemoveStopTest)}/Report.txt");

      TurnOffDbTracking();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == OSM_STOP_ID_3);
      Assert.True(actualStop1.IsDeleted);

      ReportTile actualReportTile =
        _dbContext.ChangeReports.FirstOrDefault(x => x.TileId == tile.Id).TileReport;

      ReportTile expectedReportTile =
        SerializationHelper.JsonDeserialize<ReportTile>($"{TestDataFolder}{nameof(RemoveStopTest)}/ReportTile.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));

      TurnOnDbTracking();
    }

    [Fact]
    public async Task RemoveStopTwiceTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());
      report = await Put_Tile_UpdateStops(tile.Id.ToString());

      Assert.Contains("No changes", report.Value);
    }
  }
}