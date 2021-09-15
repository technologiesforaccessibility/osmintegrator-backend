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
    public class StopTests : IntegrationTest
    {
        public StopTests(ApiWebApplicationFactory fixture)
          : base(fixture) { }

        private const int InitialStopQuantity = 20801;

        [Fact]
        public async Task Get_GetAllTestAsync()
        {
            HttpResponseMessage response;

            var helper = new StopHelper(_factory.CreateClient());

            response = await helper.Get_GetAllTestAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.True(false, "Can't create a new connection.");
            }
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<Stop[]>(jsonResponse).ToList();
            var connectionCount = list.Count;
            if (connectionCount != InitialStopQuantity)
            {
                Assert.True(false, $"Connection quantity is {connectionCount} but should be {InitialStopQuantity}.");
            }

            Assert.True(true);
        }
    }
}
