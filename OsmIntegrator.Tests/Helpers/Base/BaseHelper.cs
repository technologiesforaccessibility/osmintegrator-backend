using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Auth;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using osmintegrator;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;

namespace OsmIntegrator.Tests.Helpers.Base
{
    public class BaseHelper
    {
        protected const string HttpAddressAndPort = "https://localhost:44388";
        protected HttpClient _client;

        public async Task<HttpResponseMessage> LoginAsync(LoginData loginData)
        {
            var jsonLoginData = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Account/Login", content);

            return response;
        }


        public BaseHelper(HttpClient factoryClient)
        {
            GetClient(factoryClient);
        }

        public BaseHelper(HttpClient factoryClient, LoginData loginData)
        {
            GetClient(factoryClient);
            var token = GetTokenAsync(loginData).Result;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private void GetClient(HttpClient factoryClient)
        { 
            _client = factoryClient;
        }

        private async Task<TokenData> GetTokenDataAsync(LoginData loginData)
        {
            var response = await LoginAsync(loginData);
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenData>(json);

            return tokenData;
        }

        private async Task<string> GetTokenAsync(LoginData loginData)
        {
            var tokenData = await GetTokenDataAsync(loginData);

            return tokenData.Token;
        }
    }
}
