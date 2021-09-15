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


        protected async Task<HttpResponseMessage> LoginAdminAsync()
        {
            var loginData = new LoginData
            {
                Email = "admin@abcd.pl",
                Password = "12345678",
            };
            var jsonLoginData = JsonConvert.SerializeObject(loginData);
            //var json = "{\"Email\":\"admin@abcd.pl\",\"Password\":\"12345678\"}";

            var content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Account/Login", content);

            return response;
        }



        public BaseHelper(HttpClient factoryClient)
        {
            _client = factoryClient;
        }





        protected async Task<TokenData> GetTokenDataAsync()
        {
            //var loginData = new LoginData
            //{
            //    Email = "admin@abcd.pl",
            //    Password = "12345678",
            //};
            //var jsonLoginData = JsonConvert.SerializeObject(loginData);
            //var content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");

            //var response = await _client.PostAsync("/api/Account/Login", content);
            var response = await LoginAdminAsync();

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenData>(json);

            return tokenData;
        }

        protected async Task<string> GetTokenAsync()
        {
            var tokenData = await GetTokenDataAsync();

            return tokenData.Token;
        }

    }
}
