using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OsmIntegrator.Database.DataInitialization;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Connections;

public class ConnectionTest : IntegrationTest
{
  private readonly ConnectionHelper _connectionHelper;

  private LoginData _defaultLoginData = new LoginData
  {
    Email = "supervisor2@abcd.pl",
    Password = "supervisor2#12345678",
  };

  public ConnectionTest(ApiWebApplicationFactory fixture) : base(fixture)
  {
    List<DbStop> gtfsStops =
      DataInitializer.GetGtfsStopsList($"Data/{nameof(ConnectionTest)}/GtfsStops.txt").ToList();
    List<DbStop> osmStops = _dataInitializer.GetOsmStopsList($"Data/{nameof(ConnectionTest)}/OsmStops.xml").ToList();

    using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
    _dataInitializer.ClearDatabase(_dbContext);
    _dataInitializer.InitializeUsers(_dbContext);
    _dataInitializer.InitializeStopsAndTiles(_dbContext, gtfsStops, osmStops);
    transaction.Commit();

    _connectionHelper = new ConnectionHelper(_dbContext);
  }

  [Fact]
  public async Task InitialConnectionListTest()
  {
    int expected = 0;

    await LoginAndAssignTokenAsync(_defaultLoginData);
    await AssignUsersToAllTiles("supervisor2", "supervisor1");
    List<Connection> connectionList =
      await Get_Connections(_connectionHelper.Tiles[0].Id.ToString());
    connectionList.Should().HaveCount(expected);
  }

  [Fact]
  public async Task CreateAllowedConnectionTest()
  {
    await LoginAndAssignTokenAsync(_defaultLoginData);
    await AssignUsersToAllTiles("supervisor2", "supervisor1");
    var connectionAction = _connectionHelper.CreateConnection(1, 4, 0);

    // Create a new connection
    HttpResponseMessage response = await Put_Connections(connectionAction);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Get current connection quantity
    List<Connection> connectionList = await Get_Connections(_connectionHelper.Tiles[0].Id.ToString());

    Assert.Single(connectionList);
  }

  [Theory]
  [InlineData(1, 2, 0, HttpStatusCode.OK)]
  [InlineData(1, 3, 0, HttpStatusCode.BadRequest)]
  [InlineData(2, 4, 0, HttpStatusCode.BadRequest)]
  [InlineData(1, 6, 0, HttpStatusCode.BadRequest)]
  [InlineData(5, 2, 0, HttpStatusCode.BadRequest)]
  [InlineData(9, 2, 0, HttpStatusCode.BadRequest)]
    
  [InlineData(1, 2, 1, HttpStatusCode.BadRequest)]
  [InlineData(1, 6, 1, HttpStatusCode.BadRequest)]
  [InlineData(5, 2, 1, HttpStatusCode.OK)]
  [InlineData(1, 10, 1, HttpStatusCode.BadRequest)]
  [InlineData(5, 4, 1, HttpStatusCode.OK)]
  [InlineData(3, 6, 1, HttpStatusCode.BadRequest)]
  public async Task SerialCreateConnectionTest(int leftStopId, int rightStopId, int tileId,
    HttpStatusCode httpStatusCode)
  {
    await LoginAndAssignTokenAsync(_defaultLoginData);
    await AssignUsersToAllTiles("supervisor2", "supervisor1");
    var connectionAction = _connectionHelper.CreateConnection(leftStopId, rightStopId, tileId);

    // Create a new connection
    HttpResponseMessage response = await Put_Connections(connectionAction);
    response.StatusCode.Should().Be(httpStatusCode);
  }

  [Fact]
  public async Task CreateAndDeleteAllowedConnectionTest()
  {
    await LoginAndAssignTokenAsync(_defaultLoginData);
    await AssignUsersToAllTiles("supervisor2", "supervisor1");
    var newConnectionAction = _connectionHelper.CreateConnection(1, 4, 0);

    // Create a new connection
    HttpResponseMessage response = await Put_Connections(newConnectionAction);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    List<Connection> connectionList = await Get_Connections(_connectionHelper.Tiles[0].Id.ToString());
    Assert.Single(connectionList);

    // Delete a new created connection
    var connectionAction = new ConnectionAction()
    {
      OsmStopId = newConnectionAction.OsmStopId,
      GtfsStopId = newConnectionAction.GtfsStopId,
    };
    response = await Post_Connections_Remove(connectionAction);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Get current connection quantity
    connectionList = await Get_Connections(_connectionHelper.Tiles[0].Id.ToString());

    Assert.Empty(connectionList);
  }
}