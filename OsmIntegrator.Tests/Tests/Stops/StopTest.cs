using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Stops
{
  public class StopTest : IntegrationTest
  {
    private LoginData _defaultLoginData = new LoginData
    {
      Email = "supervisor1@abcd.pl",
      Password = "supervisor1#12345678",
    };

    public StopTest(ApiWebApplicationFactory fixture)
      : base(fixture)
    {
      List<DbStop> gtfsStops = _dataInitializer.GetGtfsStopsList($"Data/{nameof(StopTest)}/GtfsStops.txt").ToList();
      List<DbStop> osmStops = _dataInitializer.GetOsmStopsList($"Data/{nameof(StopTest)}/OsmStops.xml").ToList();

      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      _dataInitializer.InitializeStopsAndTiles(_dbContext, gtfsStops, osmStops);

      transaction.Commit();
    }

    [Fact]
    public async Task GetAllTestAsync()
    {
      await LoginAndAssignTokenAsync(_defaultLoginData);
      HttpResponseMessage response = await _client.GetAsync("/api/Stop");
      response.StatusCode.Should().Be(HttpStatusCode.OK);

      string jsonResponse = await response.Content.ReadAsStringAsync();
      List<Stop> list = JsonConvert.DeserializeObject<Stop[]>(jsonResponse).ToList();
      int actual = list.Count;
      int expected = 10;
      Assert.Equal(expected, actual);
    }
  }
}
