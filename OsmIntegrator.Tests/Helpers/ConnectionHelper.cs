using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OsmIntegrator.Tests.Helpers
{
  public class ConnectionHelper
  {
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;

    private readonly Dictionary<int, int> _stops = new Dictionary<int, int>()
    {
      { 159541, 1 }, // 1
      { 1831941739, 0}, // 1
      { 159542, 1 }, // 1
      { 1905028012, 0 }, // 1
      { 159077, 1 }, // 2
      { 1831944331, 0 }, // 2
      { 1905039171, 0 }, // 2
      { 159076, 1 }, // 2
      { 159061, 1 }, // 2
      { 1584594015, 0} // 2
    };

    public Dictionary<int, DbStop> Stops { get; private set; }
    public List<DbTile> Tiles {get; private set; }

    public ConnectionHelper(HttpClient client, ApplicationDbContext dbContext)
    {
      _client = client;
      _dbContext = dbContext;

      Stops = GetStopsDict();
      Tiles = new List<DbTile>{
        Stops[1].Tile, Stops[9].Tile
      };
    }

    private Dictionary<int, DbStop> GetStopsDict()
    {
      Dictionary<int, DbStop> result = new Dictionary<int, DbStop>();

      List<DbStop> stops = _dbContext.Stops.Include(x => x.Tile).ToList();

      int index = 1;
      foreach (var pair in _stops)
      {
        result.Add(index, stops.First(x => x.StopId == pair.Key && (int)x.StopType == pair.Value));
        index++;
      }

      return result;
    }

    public NewConnectionAction CreateConnection(int gtfsStopId, int osmStopId, int tileId)
    {
      var connectionAction = new NewConnectionAction
      {
        OsmStopId = Stops[osmStopId].Id,
        GtfsStopId = Stops[gtfsStopId].Id,
        TileId = Tiles[tileId].Id,
      };

      return connectionAction;
    }

    private async Task<List<Tile>> GetTileListAsync()
    {
      var response = await _client.GetAsync("/api/Tile/GetTiles");
      var jsonResponse = await response.Content.ReadAsStringAsync();
      var list = JsonConvert.DeserializeObject<Tile[]>(jsonResponse).ToList();
      return list;
    }

    public async Task<List<Connection>> GetConnectionListAsync(int tileIndex)
    {
      var response = await _client.GetAsync($"/api/Connections/{Tiles[tileIndex].Id}");
      var jsonResponse = await response.Content.ReadAsStringAsync();
      var list = JsonConvert.DeserializeObject<Connection[]>(jsonResponse).ToList();

      return list;
    }

    public async Task<HttpResponseMessage> CreateConnection(NewConnectionAction connectionAction)
    {
      var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
      var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
      var response = await _client.PutAsync("/api/Connections", content);
      return response;
    }

    public async Task<HttpResponseMessage> DeleteConnection(ConnectionAction connectionAction)
    {
      var jsonConnectionAction = JsonConvert.SerializeObject(connectionAction);
      // var content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json");
      // return await _client.DeleteAsync("/api/Connections");

      HttpRequestMessage request = new HttpRequestMessage
      {
        Content = new StringContent(jsonConnectionAction, Encoding.UTF8, "application/json"),
        Method = HttpMethod.Delete,
        RequestUri = new Uri($"{_client.BaseAddress}api/Connections")
      };

      return await _client.SendAsync(request);
    }
  }
}
