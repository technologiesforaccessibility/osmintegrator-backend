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
        private LoginData _defaultLoginData = new LoginData
        {
            Email = "supervisor1@abcd.pl",
            Password = "12345678",
        };

        public Dictionary<string, ConnectionAction> GetTestConnectionDict()
        {

            var stopHelper = new StopHelper(_client, _defaultLoginData);
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
            var token = await GetTokenAsync(_defaultLoginData);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/Connections");
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var connectionList = JsonConvert.DeserializeObject<Connection[]>(jsonResponse).ToList();
            return connectionList;
        }

        public async Task<HttpResponseMessage> CreateConnection(ConnectionAction connectionAction)
        {
            var token = await GetTokenAsync(_defaultLoginData);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync("/api/Connections", content);
            return response;
        }

        public async Task<HttpResponseMessage> DeleteConnection(ConnectionAction connectionAction)
        {
            var token = await GetTokenAsync(_defaultLoginData);
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
            return response;
        }


        public ConnectionHelper(HttpClient factoryClient) : base(factoryClient)
        {
        }
    }
}
