using System;
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
  public class AddStopTest : ImportTestBase
  {
    public AddStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.AsNoTracking().First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;

      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(AddStopTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_3);
      Assert.Equal("1584594015", actualStop1.StopId.ToString());
      Assert.Equal("Brynów Dworska", actualStop1.Name);
      Assert.Equal(5, actualStop1.Tags.Count);

      TileImportReport actualReportTile =
        _dbContext.ChangeReports.AsNoTracking().FirstOrDefault(x => x.TileId == tile.Id)?.TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(AddStopTest)}/ReportTile.json");

      Assert.Empty(Compare(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}