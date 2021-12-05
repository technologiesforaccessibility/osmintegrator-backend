using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Mocks;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Imports
{
  public class OsmUpdateTest : IntegrationTest
  {
    private LoginData _defaultLoginData = new LoginData
    {
      Email = "supervisor2@abcd.pl",
      Password = "supervisor2#12345678",
    };
    public OsmUpdateTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    private void InitializeDb(string testName)
    {
      List<DbStop> osmStops =
        _dataInitializer.GetOsmStopsList($"Data/{nameof(OsmUpdateTest)}/{testName}/OsmStopsInit.xml").ToList();

      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      _dataInitializer.InitializeStopsAndTiles(_dbContext, null, osmStops);
      transaction.Commit();
    }

    [Fact]
    public async Task PositionTest()
    {
      InitializeDb(nameof(PositionTest));

      await LoginAndAssignTokenAsync(_defaultLoginData);
      await AssignUsersToAllTiles("supervisor2", "supervisor1");

      IOverpass overpass = _factory.Services.GetService<IOverpass>();
      OverpassMock overpassMock = (OverpassMock)overpass;
      overpassMock.OsmFileName = $"Data/{nameof(OsmUpdateTest)}/{nameof(PositionTest)}/OsmStopsNew.xml";

      DbTile tile = _dbContext.Tiles.First(x => x.X == 2264 && x.Y == 1385);
      HttpResponseMessage response = await UpdateTileAsync(tile.Id.ToString());

      response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public async Task<HttpResponseMessage> UpdateTileAsync(string tileId)
    {
      return await _client.PutAsync($"/api/Tile/UpdateStops/{tileId}", null);
    }
  }
}

