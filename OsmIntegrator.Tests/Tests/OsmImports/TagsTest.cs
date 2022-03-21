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
  public class TagsTest : ImportTestBase
  {
    public TagsTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(TagsTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;

      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(TagsTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_1);
      Assert.Equal("Brynów Orkana n/ż", actualStop1.Name);
      Assert.Equal("12345", actualStop1.Ref);
      Assert.Equal("2t", actualStop1.Number);

      Assert.Equal(7, actualStop1.Tags.Count);
      Assert.Contains(actualStop1.Tags, x => x.Key == "ref" && x.Value == "12345");
      Assert.Contains(actualStop1.Tags, x => x.Key == "local_ref" && x.Value == "2t");
      Assert.Contains(actualStop1.Tags, x => x.Key == "name" && x.Value == "Brynów Orkana n/ż");

      DbStop actualStop2 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_2);
      Assert.Equal(5, actualStop2.Tags.Count);
      Assert.Contains(actualStop2.Tags, x => x.Key == "very_public_transport" && x.Value == "stop_position");

      Assert.DoesNotContain(actualStop2.Tags, x => x.Key == "public_transport");

      TileImportReport actualReportTile =
        _dbContext.ChangeReports.AsNoTracking().FirstOrDefault(x => x.TileId == tile.Id)?.TileReport;

      TileImportReport expectedReportTile =
        SerializationHelper.JsonDeserialize<TileImportReport>($"{TestDataFolder}{nameof(TagsTest)}/ReportTile.json");

      Assert.Empty(Compare(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}