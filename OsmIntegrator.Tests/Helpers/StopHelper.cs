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
    public class StopHelper : BaseHelper
    {
        public Dictionary<int, Stop> GetTestStopDict()
        {
            var list = new List<int>
            {
                159541,         // 1 - Gtfs
                1831941739,     // 2 - Osm
                159542,         // 3
                1905028012,     

                159077,
                1831944331,
                1905039171,
                159076,

                159061,
                1584594015,     // 10
            };

            var dbStopList = GetStopListAsync().Result;

            return list
                .Select((x, index) => new { Key = ++index, Value = x })
                .Join(
                    dbStopList,
                    l => l.Value,
                    r => r.StopId,
                    (l, r) => new { l.Key, Value = r }
                    )
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<HttpResponseMessage> Get_GetAllTestAsync()
        {
            var response = await _client.GetAsync("/api/Stop");
            return response;
        }

        public StopHelper(HttpClient factoryClient, LoginData loginData) : base(factoryClient, loginData)
        {
        }

        private async Task<List<Stop>> GetStopListAsync()
        {
            var response = await Get_GetAllTestAsync();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<Stop[]>(jsonResponse).ToList();

            return list;
        }
    }
}
