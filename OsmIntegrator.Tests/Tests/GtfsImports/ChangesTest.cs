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

namespace OsmIntegrator.Tests.Tests.GtfsImports
{
  public class ChangesTest : ImportTestBase
  {
    public ChangesTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task ChangeNameAndNumberTest()
    {
      await InitTest(nameof(ChangesTest), "supervisor2", "supervisor1");

      DbStop expectedStop1 = GetExpectedStop(GTFS_STOP_ID_1, null, null, "Stara Ligota Rolna1");
      DbStop expectedStop2 = GetExpectedStop(GTFS_STOP_ID_2, null, null, null, "3");
      DbStop expectedStop3 = GetExpectedStop(GTFS_STOP_ID_3, null, null, "Brynów Orkana1", "3");

      MultipartFormDataContent content = new MultipartFormDataContent();
      StreamContent fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(ChangesTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(ChangesTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_1);
      Assert.Equal(expectedStop1.Name, actualStop1.Name);

      DbStop actualStop2 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_2);
      Assert.Equal(expectedStop2.Number, actualStop2.Number);

      DbStop actualStop3 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_3);
      Assert.Equal(expectedStop3.Name, actualStop3.Name);
      Assert.Equal(expectedStop3.Number, actualStop3.Number);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(ChangesTest)}/Report.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }
  }
}