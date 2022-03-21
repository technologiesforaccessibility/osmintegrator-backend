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
using System.Net.Http;

namespace OsmIntegrator.Tests.Tests.GtfsImports
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

      MultipartFormDataContent content = new MultipartFormDataContent();
      StreamContent fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(AddStopTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;

      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(AddStopTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbStop actualStop1 = _dbContext.Stops.AsNoTracking().First(x => x.StopId == 111111);
      Assert.Equal("111111", actualStop1.StopId.ToString());
      Assert.Equal("Brynów Dworska", actualStop1.Name);
      Assert.Equal("1", actualStop1.Number);

      GtfsImportReport actualReport =
        _dbContext.GtfsImportReports.AsNoTracking()
        .OrderBy(x => x.CreatedAt)
        .Last()?.GtfsReport;

      GtfsImportReport expectedReport =
        SerializationHelper.JsonDeserialize<GtfsImportReport>($"{TestDataFolder}{nameof(AddStopTest)}/Report.json");

      Assert.Empty(Compare(expectedReport, actualReport));
    }
  }
}