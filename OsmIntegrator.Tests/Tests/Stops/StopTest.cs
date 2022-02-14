using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OsmIntegrator.Database.DataInitialization;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Stops
{
  public class StopTest : StopsTestBase
  {
    private LoginData _defaultLoginData = new LoginData
    {
      Email = "supervisor1@abcd.pl",
      Password = "supervisor1#12345678",
    };

    public StopTest(ApiWebApplicationFactory fixture)
      : base(fixture)
    {
      List<DbStop> gtfsStops = DataInitializer.GetGtfsStopsList($"Data/{nameof(StopTest)}/GtfsStops.txt").ToList();
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
      List<Stop> list = await GetAllStops();
      int actual = list.Count;
      int expected = 10;
      Assert.Equal(expected, actual);
    }
  }
}
