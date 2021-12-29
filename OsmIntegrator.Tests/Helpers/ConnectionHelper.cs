using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Connections;
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
      // Stop id / Stop type
      { 159541, 1 }, // Tile: 1
      { 1831941739, 0}, // Tile: 1
      { 159542, 1 }, // Tile: 1
      { 1905028012, 0 }, // Tile: 1
      { 159077, 1 }, // Tile: 2
      { 1831944331, 0 }, // Tile: 2
      { 1905039171, 0 }, // Tile: 2
      { 159076, 1 }, // Tile: 2
      { 159061, 1 }, // Tile: 2
      { 1584594015, 0} // Tile: 2
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
  }
}
