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
  public class RemoveStopTest : ImportTestBase
  {
    public RemoveStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RemoveSingleStopTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RemoveStopTest)}/Report.txt");
      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_3);
      Assert.True(actualStop1.IsDeleted);

      List<DbConnection> deletedConnections = _dbContext.Connections
        .Include(x => x.OsmStop)
        .Where(x => x.OsmStop.IsDeleted)
        .ToList();

      Assert.Empty(deletedConnections);

      TileImportReport actualReportTile =
        _dbContext.ChangeReports.AsNoTracking().FirstOrDefault(x => x.TileId == tile.Id)?.TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(RemoveStopTest)}/ReportTile.json");

      Assert.Empty(Compare(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }

    [Fact]
    public async Task RemoveStopTwiceTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.AsNoTracking().First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());
      report = await Put_Tile_UpdateStops(tile.Id.ToString());

      Assert.Contains("Brak zmian", report.Value);
    }
  }
}