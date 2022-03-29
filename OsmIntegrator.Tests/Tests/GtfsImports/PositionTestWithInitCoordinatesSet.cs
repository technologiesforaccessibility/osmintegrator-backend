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
  public class PositionTestWithInitCoordinatesSet : ImportTestBase
  {
    private const double EXPECTED_LAT = 50.231382;
    private const double EXPECTED_LON = 18.982616;
    private const double EXPECTED_INIT_LAT = 50.231381;
    private const double EXPECTED_INIT_LON = 18.982615;

    public PositionTestWithInitCoordinatesSet(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task TestPositionChangeWithInitCoordinatesSet()
    {
      await InitTest(nameof(PositionTestWithInitCoordinatesSet), "supervisor2", "supervisor1");

      DbStop expectedStop1 = GetExpectedStop(ExpectedValues.GTFS_STOP_ID_1, EXPECTED_LAT, EXPECTED_LON);
      expectedStop1.InitLat = EXPECTED_INIT_LAT;
      expectedStop1.InitLon = EXPECTED_INIT_LON;

      StopPositionData data = new() { StopId = expectedStop1.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };
      await ChangePosition(data);

      MultipartFormDataContent content = new MultipartFormDataContent();
      StreamContent fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(PositionTestWithInitCoordinatesSet)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(PositionTestWithInitCoordinatesSet)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);
      Assert.Equal(expectedStop1.Lat, actualStop1.Lat);
      Assert.Equal(expectedStop1.Lon, actualStop1.Lon);
      Assert.Equal(expectedStop1.InitLat, actualStop1.InitLat);
      Assert.Equal(expectedStop1.InitLon, actualStop1.InitLon);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(PositionTestWithInitCoordinatesSet)}/Report.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }
  }
}