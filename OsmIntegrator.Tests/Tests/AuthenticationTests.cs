using FluentAssertions;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests
{
    public class AuthenticationTests : IntegrationTest
    {
        public AuthenticationTests(ApiWebApplicationFactory fixture)
          : base(fixture) { }


        [Fact]
        public async Task LoginAdminTest()
        {
            var helper = new AuthenticationHelper(_factory.CreateClient());
            var response = await helper.LoginUserAdminAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #region Waste

        /*
        [Fact]
        public async Task Post_LoginAdmin()
        {
            var client = _factory.CreateClient();

            // Add a new Request Message
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:44388/api/Account/Login"),                //RequestUri = new Uri("/api/Account/Login") do not work,
                Content = new StringContent("{\"Email\":\"admin@abcd.pl\",\"Password\":\"12345678\"}", Encoding.UTF8, "application/json"),
            };

            var response = await client.SendAsync(requestMessage);
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenData>(json);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        //[Fact]
        //public async Task Get_Should_Return_Forecast()
        //{
        //    var response = await _client.GetAsync("/weatherforecast");
        //    response.StatusCode.Should().Be(HttpStatusCode.OK);

        //    var forecast = JsonConvert.DeserializeObject<WeatherForecast[]>(
        //      await response.Content.ReadAsStringAsync()
        //    );
        //    //forecast.Should().HaveCount(7);
        //    forecast.Should().HaveCount(1);
        //}

        [Fact]
        public async Task Get_Should_ResultInABadRequest_When_ConfigIsInvalid()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Connections");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Connections");
            var actual = await response.Content.ReadAsStringAsync();
            string expected = "[{\"date\":\"2021-10-14T00:00:00\",\"temperatureC\":11,\"temperatureF\":51,\"summary\":\"Bracing\"}]";

            // Assert
            //var res = actual.Equals(expected);
            //Assert.True(cont.Equals(expected));
            Assert.Equal(actual, expected);
        }
        */
        /*
        [Fact]
        public async Task GetAllConnectionsTest()
        {
            var client = _factory.CreateClient();

            var token = await GetTokenAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/Connections");

            //response.StatusCode.Should().Be(HttpStatusCode.OK);

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var connectionList = JsonConvert.DeserializeObject<Connection[]>(jsonResponse);
            connectionList.Should().HaveCount(7);

            //string expected = "[{\"id\":\"31fd182e-b524-4e14-9ff9-3c2e1cec1a77\",\"osmStopId\":\"165c352c-fe51-4efe-b77e-84e134e63f30\",\"osmStop\":null,\"gtfsStopId\":\"03d396ef-ec33-4cf1-89da-558713132c6e\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"f2cb8ad3-a63b-4465-998a-95f21e250548\",\"osmStopId\":\"6765a389-31f3-47cb-bb48-e1140511bee0\",\"osmStop\":null,\"gtfsStopId\":\"56c0f8c8-c0b6-49f8-988e-01da5e426108\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"69dcdad5-9b88-48ca-beb1-2c655bcf6794\",\"osmStopId\":\"0461c0a1-4248-4a80-873b-765a42e4c385\",\"osmStop\":null,\"gtfsStopId\":\"595caf78-09e5-4078-b229-470b1caeb7d2\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"4e74880a-a696-4f21-9418-d1b53b4bdcde\",\"osmStopId\":\"b6faa8ff-c89d-400c-b686-fc7b275b3e94\",\"osmStop\":null,\"gtfsStopId\":\"a5617a74-85fb-430d-8e64-b2acd512bfbc\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"85d5a94b-0a6c-4c0b-92ad-ba757567fad2\",\"osmStopId\":\"dcd4d470-337a-45fe-8d5a-bf5a471f7971\",\"osmStop\":null,\"gtfsStopId\":\"a6238de3-4638-4eba-ad79-916d2f00d13f\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"7e9b0823-3380-4455-a489-583a117748e0\",\"osmStopId\":\"d099c4b1-381c-417b-94a7-895282739375\",\"osmStop\":null,\"gtfsStopId\":\"a6b369d6-5850-4353-9214-89f8363160a3\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"a1fa780b-f521-44e3-9c43-beb1f740f91d\",\"osmStopId\":\"dfac8d4e-2b25-48ee-bdba-dc52b4560abc\",\"osmStop\":null,\"gtfsStopId\":\"d39df65f-5694-4a03-92e6-667f60a74da7\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null}]";            
            //Assert.Equal(actual, expected);
        }
        */
        /*
        [Fact]
        public async Task CreateConnectionTest()
        {
            var client = _factory.CreateClient();

            var token = await GetTokenAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var connectionAction = new ConnectionAction
            {
                OsmStopId = new Guid("64921339-47ed-4b22-8e96-31adda7133e0"),
                GtfsStopId = new Guid("2c6f0e83-3a27-43a1-9771-bd3748728dda"),
            };

            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);

            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("/api/Connections", content);
            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }



        [Fact]
        public async Task DeleteConnectionTest()
        {
            var client = _factory.CreateClient();
            var token = await GetTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var connectionAction = new ConnectionAction
            {
                OsmStopId = new Guid("64921339-47ed-4b22-8e96-31adda7133e0"),
                GtfsStopId = new Guid("2c6f0e83-3a27-43a1-9771-bd3748728dda"),
            };
            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");

            // Add a new Request Message
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("https://localhost:44388/api/Connections"),                //RequestUri = new Uri("/api/Account/Login") do not work,
                Content = content,
            };

            var response = await client.SendAsync(requestMessage);
            var json = await response.Content.ReadAsStringAsync();
            //var tokenData = JsonConvert.DeserializeObject<TokenData>(json);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        */

        //[Fact]
        //public async Task CreateConnectionBasedOnNotConnectedStops()
        //{
        //    var client = _factory.CreateClient();
        //    //var response = await client.PostAsync("/api/Connections", new HttpContent(""));
        //    //response.StatusCode.Should().Be(HttpStatusCode.OK);
        //}

        #endregion
    }
}
