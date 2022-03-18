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
  public class RemoveStopTest : ImportTestBase
  {
    public RemoveStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RemoveSingleStopTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RemoveStopTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RemoveStopTest)}/Report.txt");
      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_3);
      Assert.True(actualStop1.IsDeleted);

      List<DbConnection> deletedConnections = _dbContext.Connections
        .Include(x => x.OsmStop)
        .Where(x => x.OsmStop.IsDeleted)
        .ToList();

      Assert.Empty(deletedConnections);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(RemoveStopTest)}/Report.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }

    [Fact]
    public async Task RemoveStopTwiceTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RemoveStopTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");
      Report report = await Put_UpdateGtfsStops(content);
      report = await Put_UpdateGtfsStops(content);

      Assert.Contains("Brak zmian", report.Value);
    }
  }
}