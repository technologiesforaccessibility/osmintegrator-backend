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
using Xunit;

namespace OsmIntegrator.Tests.Tests.OsmImports
{
  public class AddStopTest : ImportTestBase
  {
    public AddStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      Report report = await UpdateTileAsync(tile.Id.ToString());

      string actualTxtReport = report.Value;

      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(AddStopTest)}/Report.txt");

      TurnOffDbTracking();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == OSM_STOP_ID_3);
      Assert.Equal("1584594015", actualStop1.StopId.ToString());
      Assert.Equal("Brynów Dworska", actualStop1.Name);
      Assert.Equal(5, actualStop1.Tags.Count);

      ReportTile actualReportTile =
        _dbContext.ChangeReports.FirstOrDefault(x => x.TileId == tile.Id).TileReport;

      ReportTile expectedReportTile =
        Deserialize<ReportTile>($"{TestDataFolder}{nameof(AddStopTest)}/ReportTile.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}