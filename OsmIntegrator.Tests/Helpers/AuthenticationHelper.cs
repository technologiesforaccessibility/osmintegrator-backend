using OsmIntegrator.Tests.Helpers.Base;
using System.Net.Http;

namespace OsmIntegrator.Tests.Helpers
{
    public class AuthenticationHelper : BaseHelper
    {

        public AuthenticationHelper(HttpClient factoryClient) : base(factoryClient)
        {
        }
    }
}
