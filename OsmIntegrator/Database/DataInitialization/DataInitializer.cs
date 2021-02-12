using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using OsmIntegrator.Database.Models;
using System.Xml.Serialization;
using OsmIntegrator.Tools;
using System.IO;
using System;
using Microsoft.Extensions.Configuration;

namespace OsmIntegrator.Database.DataInitialization
{
    public class DataInitializer
    {

        private readonly int _zoomLevel;

        public DataInitializer(IConfiguration configuration)
        {
            _zoomLevel = int.Parse(configuration["ZoomLevel"]);
        }

        public List<Stop> GetGtfsStopsList()
        {
            List<string[]> csvStopList = CsvParser.Parse("Files/GtfsStops.txt");
            List<Stop> ztmStopList = csvStopList.Select((x, index) => new Stop()
            {
                Id = Guid.NewGuid(),
                StopId = long.Parse(x[0]),
                Number = x[1],
                Name = x[2],
                Lat = double.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
                Lon = double.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat),
                StopType = StopType.Gtfs,
                ProviderType = ProviderType.Ztm
            }).ToList();
            return ztmStopList;
        }

        public (List<Stop> Stops, List<Models.Tag> Tags) GetOsmStopsList()
        {
            List<Stop> result = new List<Stop>();
            List<Models.Tag> tags = new List<Models.Tag>();

            XmlSerializer serializer =
                new XmlSerializer(typeof(Osm));

            using (Stream reader = new FileStream("Files/OsmStops.xml", FileMode.Open))
            {
                Osm osmRoot = (Osm)serializer.Deserialize(reader);

                foreach (Node node in osmRoot.Node)
                {
                    Stop stop = new Stop
                    {
                        Id = Guid.NewGuid(),
                        StopId = long.Parse(node.Id),
                        Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
                        Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
                        StopType = StopType.Osm,
                        ProviderType = ProviderType.Ztm
                    };

                    List<Models.Tag> tempTags = new List<Models.Tag>();

                    node.Tag.ForEach(x => tempTags.Add(new Models.Tag()
                    {
                        Id = Guid.NewGuid(),
                        OsmStopId = stop.Id,
                        Key = x.K,
                        Value = x.V
                    }));

                    tags.AddRange(tempTags);

                    var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "name");
                    stop.Name = nameTag?.Value;
                    result.Add(stop);
                }

            }

            return (result, tags);
        }

        public List<Tile> GetTiles(List<Stop> stops)
        {
            HashSet<Tile> result = new HashSet<Tile>();

            foreach(Stop stop in stops)
            {
                Point tileCoordinates = TilesHelper.WorldToTilePos(stop.Lon, stop.Lat, _zoomLevel);
                Point gpsCoordinates = TilesHelper.TileToWorldPos(
                    (long)tileCoordinates.X, (long)tileCoordinates.Y, _zoomLevel);
                Tile t = new Tile()
                {
                    Id = Guid.NewGuid(),
                    X = (long)tileCoordinates.X,
                    Y = (long)tileCoordinates.Y,
                    Lon = gpsCoordinates.X,
                    Lat = gpsCoordinates.Y
                };

                Tile desiredTile;
                if(result.TryGetValue(t, out desiredTile))
                {
                    stop.TileId = desiredTile.Id;
                } else
                {
                    stop.TileId = t.Id;
                    result.Add(t);
                }    
            }

            return result.ToList();
        }
    }
}
