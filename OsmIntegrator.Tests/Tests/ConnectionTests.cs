using FluentAssertions;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
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
            Password = "12345678",
        };


        [Fact]
        public async Task InitialConnectionListTest()
        {
            var initialConnectionQuantity = 7;

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);
            var connectionList = await helper.GetConnectionListAsync();
            connectionList.Should().HaveCount(initialConnectionQuantity);
        }

        [Fact]
        public async Task CreateConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);


            var connectionDict = helper.GetTestConnectionDict();
            var connectionAction = connectionDict["1-4"];

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

        [Fact]
        public async Task CreateAndDeleteConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            var helper = new ConnectionHelper(_factory.CreateClient(), _defaultLoginData);

            var connectionDict = helper.GetTestConnectionDict();
            var connectionAction = connectionDict["1-4"];

            // Get an intitial connetionquantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;

            // Create a new connection
            response = await helper.CreateConnection(connectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }

            // Delete a new created connection
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
