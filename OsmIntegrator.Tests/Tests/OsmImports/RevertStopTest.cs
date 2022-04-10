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
  public class RevertStopTest : ImportTestBase
  {
    public RevertStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RevertOnlyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      await Put_Tile_UpdateStops(tile.Id.ToString());

      _overpassMock.OsmFileName = $"{TestDataFolder}{nameof(RevertStopTest)}/OsmStopsInit.xml";

      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.OsmImportReports.AsNoTracking().Count());
      List<DbTileImportReport> actualChangeReports =
        _dbContext.OsmImportReports.AsNoTracking().Where(x => x.TileId == tile.Id).ToList();
      TileImportReport actualReportTile = actualChangeReports.Last().TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(RevertStopTest)}/ReportTile.json");

      Assert.Empty(Compare<TileImportReport>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }

    [Fact]
    public async Task RevertAndModifyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.AsNoTracking().First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      await Put_Tile_UpdateStops(tile.Id.ToString());

      _overpassMock.OsmFileName = $"{TestDataFolder}{nameof(RevertStopTest)}/OsmStopsNew_Modify.xml";

      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());
      File.WriteAllText("Report.txt", report.Value);
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report_Modify.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.OsmImportReports.AsNoTracking().Count());
      List<DbTileImportReport> actualChangeReports =
        _dbContext.OsmImportReports.AsNoTracking().Where(x => x.TileId == tile.Id).ToList();
      TileImportReport actualReportTile = actualChangeReports.Last().TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(RevertStopTest)}/ReportTile_Modify.json");

      Assert.Empty(Compare<TileImportReport>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}