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
  public class RemoveStopTwiceTest : ImportTestBase
  {
    public RemoveStopTwiceTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(RemoveStopTwiceTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(RemoveStopTwiceTest)}/Data.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data.txt");
      Report report = await Put_UpdateGtfsStops(content);
      report = await Put_UpdateGtfsStops(content);

      Assert.Contains("Brak zmian", report.Value);
    }
  }
}