using FluentAssertions;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
using OsmIntegrator.Tests.Tests.Base;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests
{
    public class ConnectionTests : IntegrationTest
    {
        private LoginData _defaultLoginData = new LoginData
        {
            Email = "supervisor1@abcd.pl",
            Password = "supervisor1#12345678",
        };


        [Fact]
        public async Task InitialConnectionListTest()
        {
            var initialConnectionQuantity = 0;

            TestHelper.RefillDatabase();

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);
            var connectionList = await helper.GetConnectionListAsync();
            connectionList.Should().HaveCount(initialConnectionQuantity);
        }

        [Fact]
        public async Task CreateAllowedConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            TestHelper.RefillDatabase();

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);
            var connectionAction = helper.CreateConnection(1, 4, 1); 

            // Get an intitial connetion quantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;

            // Create a new connection
            response = await helper.CreateConnection(connectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }

            // Get current connection quantity
            connectionList = await helper.GetConnectionListAsync();
            var currentConnectionCount = connectionList.Count;
            
            // Assert
            if (currentConnectionCount != initialConnectionCount + 1)
            {
                Assert.True(false, $"Connection quantity is {currentConnectionCount} but should be {initialConnectionCount + 1}.");
            }
            Assert.True(true);
        }

        [Theory]
        [InlineData(1,  2, 1, HttpStatusCode.OK)]               // two stops of differen types inside the left tile - GTFS, OSM, active left tile
        [InlineData(1,  3, 1, HttpStatusCode.BadRequest)]       // two stops of the same type inside the left tile - GTFS, GTFS, active left tile
        [InlineData(2,  4, 1, HttpStatusCode.BadRequest)]       // two stops of the same type inside the left tile - OSM, OSM, active left tile
        [InlineData(1,  2, 2, HttpStatusCode.BadRequest)]       // two stops of differen types inside the left tile - GTFS, OSM, active right tile - wrong Tile because both stops are on left Tile 
        
        [InlineData(1,  6, 1, HttpStatusCode.BadRequest)]       // GTFS inside the left tile and OSM inside the right in acceptable distance, active left tile
        [InlineData(1,  6, 2, HttpStatusCode.OK)]               // GTFS inside the left tile and OSM inside the right in acceptable distance, active right tile
        [InlineData(5,  2, 1, HttpStatusCode.OK)]               // GTFS inside the right tile and OSM inside the left tile in acceptable distance, active left tile
        [InlineData(5,  2, 2, HttpStatusCode.BadRequest)]       // GTFS inside the right tile and OSM inside the left tile in acceptable distance, active left tile

        [InlineData(1, 10, 2, HttpStatusCode.BadRequest)]       // GTFS inside the left tile and OSM inside the right in not acceptable distance, active right tile
        [InlineData(9,  2, 1, HttpStatusCode.BadRequest)]       // GTFS inside the right tile and OSM inside the left tile in not acceptable distance, active left tile
        public async Task SerialCreateConnectionTest(int leftStopId, int rightStopId, int tileId, HttpStatusCode httpStatusCode)
        {
            TestHelper.RefillDatabase();

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);
            var connectionAction = helper.CreateConnection(leftStopId, rightStopId, tileId);

            // Create a new connection
            HttpResponseMessage response = await helper.CreateConnection(connectionAction);
            response.StatusCode.Should().Be(httpStatusCode);
        }

        [Fact]
        public async Task CreateAndDeleteAllowedConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            TestHelper.RefillDatabase();

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);
            var newConnectionAction = helper.CreateConnection(1, 4, 1);

            // Get an intitial connetionquantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;

            // Create a new connection
            response = await helper.CreateConnection(newConnectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }

            // Delete a new created connection
            var connectionAction = new ConnectionAction()
            {
                OsmStopId = newConnectionAction.OsmStopId,
                GtfsStopId = newConnectionAction.GtfsStopId,
            };
            response = await helper.DeleteConnection(connectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't delete a new created connection.");
            }

            // Get current connection quantity
            connectionList = await helper.GetConnectionListAsync();
            var currentConnectionCount = connectionList.Count;
            
            // Assert
            if (currentConnectionCount != initialConnectionCount)
            {
                Assert.True(false, $"Connection quantity is {currentConnectionCount} but should be {initialConnectionCount}.");
            }
            Assert.True(true, "Created a new connection and deleted a new created collection.");
        }


        public ConnectionTests(ApiWebApplicationFactory fixture) : base(fixture) 
        { 
        }
    }
}
