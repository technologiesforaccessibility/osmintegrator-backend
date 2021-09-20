using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
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
    public class StopTests : IntegrationTest
    {
        private const int InitialStopQuantity = 20801;
        private LoginData _defaultLoginData = new LoginData
        {
            Email = "supervisor1@abcd.pl",
            Password = "12345678",
        };

        [Fact]
        public async Task Get_GetAllTestAsync()
        {
            HttpResponseMessage response;
            var helper = new StopHelper(_factory.CreateClient(), _defaultLoginData);
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


        public StopTests(ApiWebApplicationFactory fixture)
          : base(fixture) { }
    }
}
