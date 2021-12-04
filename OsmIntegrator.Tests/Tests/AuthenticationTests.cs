using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tests.Fixtures;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Tests
{
  public class AuthenticationTests : IntegrationTest
  {
    public AuthenticationTests(ApiWebApplicationFactory fixture) : base(fixture)
    {
      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      transaction.Commit();
    }

    [Theory]
    [ClassData(typeof(CalculatorTestData))]
    public async Task LoginTest(string email, string password)
    {
      var loginData = new LoginData
      {
        Email = email,
        Password = password,
      };

      var response = await LoginAsync(loginData);
      response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public class CalculatorTestData : IEnumerable<object[]>
    {
      public IEnumerator<object[]> GetEnumerator()
      {
        yield return new object[] { "admin@abcd.pl", "admin#12345678" };
        yield return new object[] { "supervisor1@abcd.pl", "supervisor1#12345678" };
        yield return new object[] { "editor1@abcd.pl", "editor1#12345678" };
        yield return new object[] { "user1@abcd.pl", "user1#12345678" };
      }

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public async Task<HttpResponseMessage> LoginAsync(LoginData loginData)
    {
      var jsonLoginData = JsonConvert.SerializeObject(loginData);
      var content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
      var response = await _client.PostAsync("/api/Account/Login", content);

      return response;
    }
  }
}
