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
  public class FileValidationTest : ImportTestBase
  {
    public FileValidationTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task TestInvalidContentType()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(FileValidationTest)}/Data_InvalidContentType.js"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/javascript");

      content.Add(fileStreamContent, "file", "Data_InvalidContentType.js");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport = "400";

      Assert.Equal(expectedTxtReport, actualTxtReport);
    }

    [Fact]
    public async Task TestInvalidHeader()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(FileValidationTest)}/Data_InvalidHeader.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data_InvalidHeader.txt");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport = "400";

      Assert.Equal(expectedTxtReport, actualTxtReport);
    }

    [Fact]
    public async Task TestNoRecords()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(FileValidationTest)}/Data_InvalidNoRecords.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data_InvalidNoRecords.txt");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport = "400";

      Assert.Equal(expectedTxtReport, actualTxtReport);
    }

    [Fact]
    public async Task TestValid()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      var content = new MultipartFormDataContent();
      var fileStreamContent = new StreamContent(File.OpenRead($"{TestDataFolder}{nameof(FileValidationTest)}/Data_Valid.txt"));
      fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

      content.Add(fileStreamContent, "file", "Data_Valid.txt");
      Report report = await Put_UpdateGtfsStops(content);

      string actualTxtReport = report.Value;
      string expectedTxtReport = "400";

      Assert.NotEqual(expectedTxtReport, actualTxtReport);
    }
  }
}