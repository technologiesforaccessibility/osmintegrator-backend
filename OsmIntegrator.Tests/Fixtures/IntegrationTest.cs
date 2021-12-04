using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OsmIntegrator.Tests.Fixtures
{
  public abstract class IntegrationTest : IClassFixture<ApiWebApplicationFactory>
  {
    protected readonly ApiWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly IConfiguration _configuration;

    protected ApplicationDbContext _dbContext;
    protected DataInitializer _dataInitializer;

    public IntegrationTest(ApiWebApplicationFactory fixture)
    {
      _factory = fixture;
      _client = _factory.CreateClient();
      _dbContext = _factory.Services.GetService<ApplicationDbContext>();
      _dataInitializer = _factory.Services.GetService<DataInitializer>();
    }

    /// <summary>
    /// Generate token and assign it to the HttpClient
    /// </summary>
    /// <param name="loginData">User and password</param>
    protected async Task LoginAndAssignTokenAsync(LoginData loginData)
    {
      string jsonLoginData = JsonConvert.SerializeObject(loginData);
      StringContent content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await _client.PostAsync("/api/Account/Login", content);
      string json = await response.Content.ReadAsStringAsync();
      TokenData tokenData = JsonConvert.DeserializeObject<TokenData>(json);

      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.Token);
    }

    protected async Task AssignUsersToAllTiles(string editorName, string supervisorName)
    {
      Guid editorId = _dbContext.Users.First(x => x.UserName == editorName).Id;
      Guid supervisorId = _dbContext.Users.First(x => x.UserName == supervisorName).Id;

      UpdateTileInput input = new UpdateTileInput
      {
        EditorId = editorId,
        SupervisorId = supervisorId
      };

      foreach (DbTile tile in _dbContext.Tiles.ToList())
      {
        string jsonLoginData = JsonConvert.SerializeObject(input);
        StringContent content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync($"/api/Tile/UpdateUsers/{tile.Id}", content);
        string json = await response.Content.ReadAsStringAsync();
      }
    }
  }

}
