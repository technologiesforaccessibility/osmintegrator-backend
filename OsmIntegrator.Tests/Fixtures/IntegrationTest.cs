﻿using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ObjectsComparer;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.ApiModels.Tiles;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tests.Mocks;
using OsmIntegrator.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using OsmIntegrator.ApiModels.Stops;
using FluentAssertions;

namespace OsmIntegrator.Tests.Fixtures
{
  public abstract class IntegrationTest : IClassFixture<ApiWebApplicationFactory>
  {
    protected string TestDataFolder { get; set; }
    protected readonly IOverpass _overpass;
    protected readonly OverpassMock _overpassMock;

    protected readonly ApiWebApplicationFactory _factory;
    protected HttpClient _client;
    protected readonly IConfiguration _configuration;
    protected ApplicationDbContext _dbContext;
    protected DataInitializer _dataInitializer;

    public IntegrationTest(ApiWebApplicationFactory factory)
    {
      // Host
      _factory = factory;
      _client = _factory.CreateClient();
      _dbContext = _factory.Services.GetService<ApplicationDbContext>();
      _dataInitializer = _factory.Services.GetService<DataInitializer>();
      _configuration = _factory.Services.GetService<IConfiguration>();

      // Overpass
      _overpass = _factory.Services.GetService<IOverpass>();
      _overpassMock = (OverpassMock)_overpass;
    }

    #region DB Initialization

    protected void InitializeDb(string testName)
    {
      List<DbStop> gtfsStops = null;
      string gtfsStopsPath = $"{TestDataFolder}{testName}/GtfsStopsInit.txt";
      if (File.Exists(gtfsStopsPath))
      {
        gtfsStops = DataInitializer.GetGtfsStopsList(gtfsStopsPath);
      }

      List<DbStop> osmStops = null;
      string osmStopPath = $"{TestDataFolder}{testName}/OsmStopsInit.xml";
      if (File.Exists(osmStopPath))
      {
        osmStops =
          _dataInitializer.GetOsmStopsList(osmStopPath).ToList();
      }

      using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
      _dataInitializer.ClearDatabase(_dbContext);
      _dataInitializer.InitializeUsers(_dbContext);
      _dataInitializer.InitializeStopsAndTiles(_dbContext, gtfsStops, osmStops);
      transaction.Commit();
    }

    protected async Task InitTest(
      string testName,
      string editorName,
      string supervisorName)
    {
      InitializeDb(testName);

      await LoginAndAssignTokenAsync(
        new LoginData
        {
          Email = editorName + "@abcd.pl",
          Password = editorName + "#12345678"
        });

      await AssignUsersToAllTiles(editorName, supervisorName);

      _overpassMock.OsmFileName = $"{TestDataFolder}{testName}/OsmStopsNew.xml";
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
        EditorId = editorId
      };

      foreach (DbTile tile in _dbContext.Tiles.ToList())
      {
        string jsonLoginData = JsonConvert.SerializeObject(input);
        StringContent content = new StringContent(jsonLoginData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync($"/api/Tile/UpdateUsers/{tile.Id}", content);
        string json = await response.Content.ReadAsStringAsync();
      }
    }

    #endregion

    #region API Client

    public async Task<OsmChange> Get_OsmExport_GetChangeFile(string tileId)
    {
      HttpResponseMessage response = await _client.GetAsync($"/api/tiles/{tileId}/export/osc");
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return SerializationHelper.XmlDeserialize<OsmChange>(jsonResponse);
    }

    public async Task<OsmChangeOutput> Get_OsmExport_GetChanges(string tileId)
    {
      HttpResponseMessage response = await _client.GetAsync($"/api/tiles/{tileId}/export/changes");
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<OsmChangeOutput>(jsonResponse);
    }

    public async Task<List<Tile>> Get_Tile_GetTiles()
    {
      HttpResponseMessage response = await _client.GetAsync("/api/Tile/GetTiles");
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Tile[]>(jsonResponse).ToList();
    }

    public async Task<List<Connection>> Get_Connections(string tileId)
    {
      HttpResponseMessage response = await _client.GetAsync($"/api/Connections/{tileId}");
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Connection[]>(jsonResponse).ToList();
    }

    public async Task<HttpResponseMessage> Put_Connections(NewConnectionAction connectionAction)
    {
      string jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
      StringContent content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await _client.PutAsync("/api/Connections", content);
      return response;
    }

    public async Task<HttpResponseMessage> Post_Connections_Remove(ConnectionAction connectionAction)
    {
      string jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
      StringContent content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await _client.PostAsync("/api/Connections/Remove", content);
      return response;
    }

    public async Task<Report> Put_Tile_UpdateStops(string tileId)
    {
      HttpResponseMessage response = await _client.PutAsync($"/api/Tile/UpdateStops/{tileId}", null);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Report>(jsonResponse);
    }

    protected async Task<Stop> ChangePosition(StopPositionData stopPositionData)
    {
      var json = JsonConvert.SerializeObject(stopPositionData);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await _client.PutAsync($"/api/Stop/ChangePosition", content);
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Stop>(jsonResponse);
    }

    public async Task<Report> Put_UpdateGtfsStops(MultipartFormDataContent dataContent)
    {
      HttpResponseMessage response = await _client.PutAsync($"/api/Gtfs/UpdateStops", dataContent);

      if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        return new()
        {
          Value = "400"
        };
      }

      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Report>(jsonResponse);
    }

    public async Task<bool> Get_Tile_ContainsChanges(string tileId)
    {
      HttpResponseMessage response = await _client.GetAsync($"/api/Tile/ContainsChanges/{tileId}");
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return bool.Parse(jsonResponse);
    }

    #endregion

    #region Other

    /// <summary>
    /// The recursive objects comparer
    /// </summary>
    /// <param name="ignoredFields">List with fields or properties that shall be ignored.</param>
    /// <returns>If objects are not the same it will return list of differences otherwise empty list.</returns>
    protected List<Difference> Compare<T>(T expected, T actual,
      List<string> ignoredFields = null)
    {
      var comparer = new ObjectsComparer.Comparer<T>();

      ignoredFields ??= new List<string>();
      ignoredFields.ForEach(x => comparer.IgnoreMember(x));

      comparer.Compare(expected, actual, out IEnumerable<Difference> differences);
      return differences.ToList();
    }

    #endregion
  }
}
