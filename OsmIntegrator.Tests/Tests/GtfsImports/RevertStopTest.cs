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
  public class RevertStopTest : ImportTestBase
  {
    public RevertStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task RevertOnlyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RevertStopTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      await Put_UpdateGtfsStops(content);

      content = new MultipartFormDataContent();
      fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RevertStopTest)}/Data_Reverted.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.GtfsImportReports.AsNoTracking().Count());

      List<DbGtfsImportReport> actualChangeReports =
        _dbContext.GtfsImportReports.AsNoTracking().ToList();
      GtfsImportReport actualReport = actualChangeReports.Last().GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(RevertStopTest)}/Report.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }

    [Fact]
    public async Task RevertAndModifyTest()
    {
      await InitTest(nameof(RevertStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RevertStopTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");

      await Put_UpdateGtfsStops(content);

      content = new MultipartFormDataContent();
      fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RevertStopTest)}/Data_Reverted_Modify.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data_Reverted_Modify.txt");

      Report report = await Put_UpdateGtfsStops(content);
      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(RevertStopTest)}/Report_Modify.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == GTFS_STOP_ID_3);
      Assert.False(actualStop1.IsDeleted);
      Assert.Equal(2, _dbContext.GtfsImportReports.AsNoTracking().Count());

      List<DbGtfsImportReport> actualChangeReports =
        _dbContext.GtfsImportReports.AsNoTracking().ToList();
      GtfsImportReport actualReport = actualChangeReports.Last().GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(RevertStopTest)}/Report_Modify.json");

      Assert.Empty(Compare<GtfsImportReport>(expectedReport, actualReport));
    }
  }
}