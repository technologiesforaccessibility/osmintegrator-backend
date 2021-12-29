
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
  public class RevertStopTest : ImportTestBase
  {
    public RevertStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RevertOnlyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      await UpdateTileAsync(tile.Id.ToString());

      _overpassMock.OsmFileName = $"{TestDataFolder}{nameof(RevertStopTest)}/OsmStopsInit.xml";

      Report report = await UpdateTileAsync(tile.Id.ToString());
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      TurnOffDbTracking();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == OSM_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.ChangeReports.Count());
      List<DbChangeReport> actualChangeReports =
        _dbContext.ChangeReports.Where(x => x.TileId == tile.Id).ToList();
      ReportTile actualReportTile = actualChangeReports.Last().TileReport;

      ReportTile expectedReportTile =
        Deserialize<ReportTile>($"{TestDataFolder}{nameof(RevertStopTest)}/ReportTile.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));

      TurnOnDbTracking();
    }

    [Fact]
    public async Task RevertAndModifyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      await UpdateTileAsync(tile.Id.ToString());

      _overpassMock.OsmFileName = $"{TestDataFolder}{nameof(RevertStopTest)}/OsmStopsNew_Modify.xml";

      Report report = await UpdateTileAsync(tile.Id.ToString());
      File.WriteAllText("Report.txt", report.Value);
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report_Modify.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      TurnOffDbTracking();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == OSM_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.ChangeReports.Count());
      List<DbChangeReport> actualChangeReports =
        _dbContext.ChangeReports.Where(x => x.TileId == tile.Id).ToList();
      ReportTile actualReportTile = actualChangeReports.Last().TileReport;

      ReportTile expectedReportTile =
        Deserialize<ReportTile>($"{TestDataFolder}{nameof(RevertStopTest)}/ReportTile_Modify.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));

      TurnOnDbTracking();
    }
  }
}