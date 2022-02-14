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
    private readonly ApplicationDbContext _dbContext;

    private readonly Dictionary<int, int> _stops = new()
    {
      { 159541, 1 },      // [1]  Tile: 0 = LEFT    GTFS:   Stara Ligota Rolna 1
      { 1831941739, 0},   // [2]  Tile: 0 = LEFT    OSM:    Stara Ligota Rolna
      { 159542, 1 },      // [3]  Tile: 0 = LEFT    GTFS:   Stara Ligota Rolna 2  In margin of the right tile
      { 1905028012, 0 },  // [4]  Tile: 0 = LEFT    OSM:    Stara Ligota Rolna    In margin of the right tile
      { 159077, 1 },      // [5]  Tile: 1 = RIGHT   GTFS:   Brynów Orkana 2
      { 1831944331, 0 },  // [6]  Tile: 1 = RIGHT   OSM:    Brynów Orkana
      { 1905039171, 0 },  // [7]  Tile: 1 = RIGHT   OSM:    Brynów Orkana
      { 159076, 1 },      // [8]  Tile: 1 = RIGHT   GTFS:   Brynów Orkana 1
      { 159061, 1 },      // [9]  Tile: 1 = RIGHT   GTFS:   Brynów Dworska 1
      { 1584594015, 0}    // [10] Tile: 1 = RIGHT   OSM:    Brynów Dworska
    };

    private Dictionary<int, DbStop> Stops { get; }
    public List<DbTile> Tiles {get; }

    public ConnectionHelper(ApplicationDbContext dbContext)
    {
      _dbContext = dbContext;

      Stops = GetStopsDict();
      Tiles = new List<DbTile>{
        Stops[1].Tile, Stops[9].Tile
      };
    }

    private Dictionary<int, DbStop> GetStopsDict()
    {
      Dictionary<int, DbStop> result = new();

      List<DbStop> stops = _dbContext.Stops.Include(x => x.Tile).ToList();

      int index = 1;
      foreach ((int key, int value) in _stops)
      {
        result.Add(index, stops.First(x => x.StopId == key && (int)x.StopType == value));
        index++;
      }

      return result;
    }

    public NewConnectionAction CreateConnection(int gtfsStopId, int osmStopId, int tileId)
    {
      NewConnectionAction connectionAction = new()
      {
        OsmStopId = Stops[osmStopId].Id,
        GtfsStopId = Stops[gtfsStopId].Id,
        TileId = Tiles[tileId].Id,
      };

      return connectionAction;
    }
  }
}
