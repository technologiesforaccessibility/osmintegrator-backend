using FluentAssertions;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tests.Helpers;
using OsmIntegrator.Tests.Helpers.Base;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests
{
    public class AuthenticationTests : IntegrationTest
    {
        public AuthenticationTests(ApiWebApplicationFactory fixture)
          : base(fixture) { }

        [Theory]
        [ClassData(typeof(CalculatorTestData))]
        public async Task LoginTest(string email, string password)
        {
            var loginData = new LoginData
            {
                Email = email,
                Password = password,
            };

            var helper = new BaseHelper(_factory.CreateClient());
            var response = await helper.LoginAsync(loginData);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        public class CalculatorTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "admin@abcd.pl", "12345678" };
                yield return new object[] { "supervisor1@abcd.pl", "12345678" };
                yield return new object[] { "editor1@abcd.pl", "12345678" };
                yield return new object[] { "user1@abcd.pl", "12345678" };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
