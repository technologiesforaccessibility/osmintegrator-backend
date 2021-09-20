using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OsmIntegrator.Tests.Helpers.Base
{
    public class BaseHelper
    {
        protected const string HttpAddressAndPort = "https://localhost:44388/";
        protected HttpClient _client;

        public BaseHelper(HttpClient factoryClient)
        {
            _client = factoryClient;
        }

        public BaseHelper(HttpClient factoryClient, LoginData loginData)
        {
            _client = factoryClient;
            var token = GetTokenAsync(loginData).Result;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<HttpResponseMessage> LoginAsync(LoginData loginData)
        {
            var jsonLoginData = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Account/Login", content);

            return response;
        }

        protected async Task<TokenData> GetTokenDataAsync(LoginData loginData)
        {
            var response = await LoginAsync(loginData);

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenData>(json);

            return tokenData;
        }

        protected async Task<string> GetTokenAsync(LoginData loginData)
        {
            var tokenData = await GetTokenDataAsync(loginData);

            return tokenData.Token;
        }

    }
}
