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
using System.Net.Http;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.GtfsImports
{
  public class PositionTest : ImportTestBase
  {
    public PositionTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task TestPositionChange()
    {
      await InitTest(nameof(PositionTest), "supervisor2", "supervisor1");

      DbStop expectedStop1 = GetExpectedStop(ExpectedValues.GTFS_STOP_ID_1, ExpectedValues.EXPECTED_LAT_1);
      DbStop expectedStop2 = GetExpectedStop(ExpectedValues.GTFS_STOP_ID_2, null, ExpectedValues.EXPECTED_LON_2);
      DbStop expectedStop3 = GetExpectedStop(ExpectedValues.GTFS_STOP_ID_3, ExpectedValues.EXPECTED_LAT_3, ExpectedValues.EXPECTED_LON_3);

      MultipartFormDataContent content = new MultipartFormDataContent();
      StreamContent fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(PositionTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(PositionTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);
      Assert.Equal(expectedStop1.Lat, actualStop1.Lat);
      Assert.Equal(expectedStop1.Version, actualStop1.Version);

      DbStop actualStop2 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_2);
      Assert.Equal(expectedStop2.Lon, actualStop2.Lon);
      Assert.Equal(expectedStop2.Version, actualStop2.Version);

      DbStop actualStop3 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_3);
      Assert.Equal(expectedStop3.Lat, actualStop3.Lat);
      Assert.Equal(expectedStop3.Lon, actualStop3.Lon);
      Assert.Equal(expectedStop3.Version, actualStop3.Version);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(PositionTest)}/Report.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }

    [Fact]
    public async Task TestTileChange()
    {
      await InitTest(nameof(PositionTest), "supervisor2", "supervisor1");

      DbStop expectedStop3 = GetExpectedStop(ExpectedValues.GTFS_STOP_ID_3, ExpectedValues.EXPECTED_LAT_OTHER_TILE, ExpectedValues.EXPECTED_LON_OTHER_TILE);

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(PositionTest)}/Data_TileChange.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(PositionTest)}/Report_TileChange.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop3 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_3);
      Assert.Equal(expectedStop3.Lat, actualStop3.Lat);
      Assert.Equal(expectedStop3.Lon, actualStop3.Lon);
      Assert.Equal(expectedStop3.Version, actualStop3.Version);
      Assert.NotEqual(expectedStop3.TileId, actualStop3.TileId);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(PositionTest)}/Report_TileChange.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }
  }
}