using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Helpers.Base;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OsmIntegrator.Tests.Helpers
{
    public class TileHelper : BaseHelper
    {
        private List<string> _tileIdList;
            
        private LoginData _defaultLoginData = new LoginData
        {
            Email = "supervisor1@abcd.pl",
            Password = "12345678",
        };

        public Dictionary<int, Tile> GetTestTileDict()
        {
            //var list = new List<string>
            //{
            //    "fa71c5ff-8804-4a86-8d63-c7b1f8953829",         // 1 - left tile
            //    "e2290ef9-2c1f-4869-be6c-d2cca15328b2",         // 2 - right tile
            //    //"04328842-4556-4903-80e8-a689848dd3f1",         // 3 - another tile
            //};

            var dbList = GetTileListAsync().Result;

            return _tileIdList
                .Select((x, index) => new { Key = ++index, Value = x })
                .Join(
                    dbList,
                    l => l.Value,
                    r => r.Id.ToString(),
                    (l, r) => new { l.Key, Value = r }
                    )
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<HttpResponseMessage> Get_GetAllTestAsync()
        {
            var response = await _client.GetAsync("/api/Tile/GetTiles");
            return response;
        }

        public TileHelper(HttpClient factoryClient, LoginData loginData) : base(factoryClient, loginData)
        {
            var stopHelper = new StopHelper(_client, _defaultLoginData);
            var stopDict = stopHelper.GetTestStopDict();

            _tileIdList = new List<string>
            {
                stopDict[1].TileId.ToString(),
                stopDict[9].TileId.ToString(),
            };
        }

        private async Task<List<Tile>> GetTileListAsync()
        {
            var response = await Get_GetAllTestAsync();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<Tile[]>(jsonResponse).ToList();

            return list;
        }
    }
}
