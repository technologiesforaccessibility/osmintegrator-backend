
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

namespace OsmIntegrator.Tests.Tests.Imports
{
  public class TagsTest : OsmUpdateTest
  {
    public TagsTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(TagsTest));

      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);
      Report report = await UpdateTileAsync(tile.Id.ToString());

      string actualTxtReport = report.Value;
      //File.WriteAllText("Report.txt", actualTxtReport);
      string expectedTxtReport =
        File.ReadAllText($"{OSM_UPDATE_FOLDER}{nameof(TagsTest)}/Report.txt");

      TurnOffDbTracking();

      DbStop actualStop1 = _dbContext.Stops.First(x => x.StopId == STOP_ID_1);
      Assert.Equal("Brynów Orkana n/ż", actualStop1.Name);
      Assert.Equal("12345", actualStop1.Ref);
      Assert.Equal("2t", actualStop1.Number);

      Assert.Equal(7, actualStop1.Tags.Count);
      Assert.Contains(actualStop1.Tags, x => x.Key == "ref" && x.Value == "12345");
      Assert.Contains(actualStop1.Tags, x => x.Key == "local_ref" && x.Value == "2t");
      Assert.Contains(actualStop1.Tags, x => x.Key == "name" && x.Value == "Brynów Orkana n/ż");

      DbStop actualStop2 = _dbContext.Stops.First(x => x.StopId == STOP_ID_2);
      Assert.Equal(5, actualStop2.Tags.Count);
      Assert.Contains(actualStop2.Tags, x => x.Key == "very_public_transport" && x.Value == "stop_position");
      Assert.False(actualStop2.Tags.Any(x => x.Key == "public_transport"));

      ReportTile actualReportTile =
        _dbContext.ChangeReports.FirstOrDefault(x => x.TileId == tile.Id).TileReport;

      ReportTile expectedReportTile =
        Deserialize<ReportTile>($"{OSM_UPDATE_FOLDER}{nameof(TagsTest)}/ReportTile.json");

      Assert.Empty(Compare<ReportTile>(
        expectedReportTile, actualReportTile, new List<string> { "TileId", "DatabaseStopId" }));
    }
  }
}