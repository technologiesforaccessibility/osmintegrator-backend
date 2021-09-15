using FluentAssertions;
//using HowToTestYourCsharpWebApi.Api;
//using HowToTestYourCsharpWebApi.Api.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests
{
    public class ConnectionTests : IntegrationTest
    {
        public ConnectionTests(ApiWebApplicationFactory fixture)
          : base(fixture) { }

        private const int InitialConnectionQuantity = 7;


        [Fact]
        public async Task InitialConnectionsTest()
        {
            var helper = new ConnectionHelper(_factory.CreateClient());
            var connectionList = await helper.GetConnectionListAsync();
            connectionList.Should().HaveCount(7);
        }


        [Fact]
        public async Task CreateConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            var helper = new ConnectionHelper(_factory.CreateClient());


            var connectionDict = helper.GetTestConnectionDict();
            var connectionAction = connectionDict["1-4"];

            // Get an intitial connetionquantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            //if (connectionCount != InitialConnectionQuantity)
            //{
            //    Assert.True(false, $"Connection quantity is {connectionCount} but should be {InitialConnectionQuantity}.");
            //}

            // Create a new connection
            response = await helper.CreateConnection(connectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }

            connectionList = await helper.GetConnectionListAsync();
            var currentConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            if (currentConnectionCount != initialConnectionCount + 1)
            {
                Assert.True(false, $"Connection quantity is {currentConnectionCount} but should be {initialConnectionCount + 1}.");
            }

            Assert.True(true);
        }


        [Fact]
        public async Task DeleteConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            var helper = new ConnectionHelper(_factory.CreateClient());


            var connectionDict = helper.GetTestConnectionDict();
            var connectionAction = connectionDict["1-4"];

            // Get an intitial connetionquantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            //if (connectionCount != InitialConnectionQuantity)
            //{
            //    Assert.True(false, $"Connection quantity is {connectionCount} but should be {InitialConnectionQuantity}.");
            //}

            // Create a new connection
            response = await helper.DeleteConnection(connectionAction);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }

            connectionList = await helper.GetConnectionListAsync();
            var currentConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            if (currentConnectionCount != initialConnectionCount - 1)
            {
                Assert.True(false, $"Connection quantity is {currentConnectionCount} but should be {initialConnectionCount - 1}.");
            }

            Assert.True(true);
        }


        [Fact]
        public async Task CreateAndDeleteConnectionTest()
        {
            int initialConnectionCount;
            HttpResponseMessage response;

            var helper = new ConnectionHelper(_factory.CreateClient());


            var connectionDict = helper.GetTestConnectionDict();
            var connectionAction = connectionDict["1-4"];

            // Get an intitial connetionquantity
            var connectionList = await helper.GetConnectionListAsync();
            initialConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            //if (connectionCount != InitialConnectionQuantity)
            //{
            //    Assert.True(false, $"Connection quantity is {connectionCount} but should be {InitialConnectionQuantity}.");
            //}

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

            connectionList = await helper.GetConnectionListAsync();
            var currentConnectionCount = connectionList.Where(f => f.Imported == true).ToList().Count;
            if (currentConnectionCount != initialConnectionCount)
            {
                Assert.True(false, $"Connection quantity is {currentConnectionCount} but should be {initialConnectionCount}.");
            }

            Assert.True(true, "Created a new connection and deleted a new created collection.");
        }
    }
}
