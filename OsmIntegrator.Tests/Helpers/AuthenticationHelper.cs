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
    public class AuthenticationHelper : BaseHelper
    {

        //public async Task<HttpResponseMessage> LoginUserAdminAsync()
        //{
        //    return await LoginAdminAsync();
        //}

        public AuthenticationHelper(HttpClient factoryClient) : base(factoryClient)
        {
        }

    }
}
