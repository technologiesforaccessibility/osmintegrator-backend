using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Helpers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OsmIntegrator.Tests.Helpers
{
    public class ConnectionHelper : BaseHelper
    {

        public Dictionary<string, ConnectionAction> GetTestConnectionDict()
        {

            var stopHelper = new StopHelper(_client);
            var stopDict = stopHelper.GetTestStopDict();

            var dict = new Dictionary<string, ConnectionAction>();

            dict.Add(
                "1-2",
                new ConnectionAction
                {
                    OsmStopId = stopDict[1].Id,            // 159541
                    GtfsStopId = stopDict[2].Id,           // 1831941739
                });
            dict.Add(
                "1-4",
                new ConnectionAction
                {
                    OsmStopId = stopDict[1].Id,            // 159541
                    GtfsStopId = stopDict[4].Id,           // 159542
                });

            return dict;
        }


        public async Task<List<Connection>> GetConnectionListAsync()
        {
            //var client = _factory.CreateClient();

            //var token = "....."     // Token taken from Postman
            //var tokenData = await GetTokenDataAsync();
            //var token = tokenData.Token;
            var token = await GetTokenAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/Connections");

            //response.StatusCode.Should().Be(HttpStatusCode.OK);

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var connectionList = JsonConvert.DeserializeObject<Connection[]>(jsonResponse).ToList();
            //connectionList.Should().HaveCount(7);

            //string expected = "[{\"id\":\"31fd182e-b524-4e14-9ff9-3c2e1cec1a77\",\"osmStopId\":\"165c352c-fe51-4efe-b77e-84e134e63f30\",\"osmStop\":null,\"gtfsStopId\":\"03d396ef-ec33-4cf1-89da-558713132c6e\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"f2cb8ad3-a63b-4465-998a-95f21e250548\",\"osmStopId\":\"6765a389-31f3-47cb-bb48-e1140511bee0\",\"osmStop\":null,\"gtfsStopId\":\"56c0f8c8-c0b6-49f8-988e-01da5e426108\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"69dcdad5-9b88-48ca-beb1-2c655bcf6794\",\"osmStopId\":\"0461c0a1-4248-4a80-873b-765a42e4c385\",\"osmStop\":null,\"gtfsStopId\":\"595caf78-09e5-4078-b229-470b1caeb7d2\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"4e74880a-a696-4f21-9418-d1b53b4bdcde\",\"osmStopId\":\"b6faa8ff-c89d-400c-b686-fc7b275b3e94\",\"osmStop\":null,\"gtfsStopId\":\"a5617a74-85fb-430d-8e64-b2acd512bfbc\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"85d5a94b-0a6c-4c0b-92ad-ba757567fad2\",\"osmStopId\":\"dcd4d470-337a-45fe-8d5a-bf5a471f7971\",\"osmStop\":null,\"gtfsStopId\":\"a6238de3-4638-4eba-ad79-916d2f00d13f\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"7e9b0823-3380-4455-a489-583a117748e0\",\"osmStopId\":\"d099c4b1-381c-417b-94a7-895282739375\",\"osmStop\":null,\"gtfsStopId\":\"a6b369d6-5850-4353-9214-89f8363160a3\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null},{\"id\":\"a1fa780b-f521-44e3-9c43-beb1f740f91d\",\"osmStopId\":\"dfac8d4e-2b25-48ee-bdba-dc52b4560abc\",\"osmStop\":null,\"gtfsStopId\":\"d39df65f-5694-4a03-92e6-667f60a74da7\",\"gtfsStop\":null,\"imported\":true,\"userId\":null,\"user\":null,\"operationType\":0,\"updatedAt\":null,\"createdAt\":\"2021-09-11T11:20:20.581903\",\"approvedBy\":null}]";            
            //Assert.Equal(actual, expected);

            return connectionList;
        }

        public async Task<HttpResponseMessage> CreateConnection(ConnectionAction connectionAction)
        {
            //var client = _factory.CreateClient();

            // Authentitacion
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync("/api/Connections", content);
            //var json = await response.Content.ReadAsStringAsync();

            return response;
        }

        public async Task<HttpResponseMessage> DeleteConnection(ConnectionAction connectionAction)
        {
            // Authentitacion
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");

            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{HttpAddressAndPort}api/Connections"),
                Content = content,
            };

            var response = await _client.SendAsync(requestMessage);
            //var json = await response.Content.ReadAsStringAsync();

            return response;
        }



        public ConnectionHelper(HttpClient factoryClient) : base(factoryClient)
        {
        }
    }
}
