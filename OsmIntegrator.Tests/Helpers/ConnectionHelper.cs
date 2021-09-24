using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database.Models;
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
        private const string Route = "/api/Connections";
        private LoginData _defaultLoginData = new LoginData
        {
            Email = "supervisor1@abcd.pl",
            Password = "12345678",
        };

        public NewConnectionAction CreateConnection(int gtfsStopId, int osmStopId, int tileId)
        { 

            var stopHelper = new StopHelper(_client, _defaultLoginData);
            var stopDict = stopHelper.GetTestStopDict();
            var tileHelper = new TileHelper(_client, _defaultLoginData);
            var tileDict = tileHelper.GetTestTileDict();

            var connectionAction = new NewConnectionAction
            {
                OsmStopId = stopDict[osmStopId].Id,
                GtfsStopId = stopDict[gtfsStopId].Id,
                TileId = tileDict[tileId].Id,
            };

            return connectionAction;
        }

        public async Task<List<DbConnections>> GetConnectionListAsync()
        {
            var response = await _client.GetAsync(Route);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<DbConnections[]>(jsonResponse).Where(f => f.OperationType==Enums.ConnectionOperationType.Added).ToList();

            return list;
        }

        public async Task<HttpResponseMessage> CreateConnection(NewConnectionAction connectionAction)
        {
            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(Route, content);
            return response;
        }

        public async Task<HttpResponseMessage> DeleteConnection(ConnectionAction connectionAction)
        {
            var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
            var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");

            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{HttpAddressAndPort}{Route}"),
                Content = content,
            };
            var response = await _client.SendAsync(requestMessage);
            return response;
        }


        public ConnectionHelper(HttpClient factoryClient, LoginData loginData) : base(factoryClient, loginData)
        {
        }
    }
}
