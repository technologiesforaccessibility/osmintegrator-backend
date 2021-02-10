using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using OsmIntegrator.Database.Models;
using System.Xml.Serialization;
using OsmIntegrator.Tools;
using System.IO;
using System;

namespace OsmIntegrator.Database.DataInitialization
{
    public class DataInitializer
    {
        public static List<GtfsStop> GetGtfsStopsList()
        {
            List<string[]> csvStopList = CsvParser.Parse("Files/GtfsStops.txt");
            List<GtfsStop> ztmStopList = csvStopList.Select((x, index) => new GtfsStop()
            {
                Id = Guid.NewGuid(),
                StopId = long.Parse(x[0]),
                Number = x[1],
                Name = x[2],
                Lat = double.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
                Lon = double.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat)
            }).ToList();
            return ztmStopList;
        }

        public static (List<OsmStop> Stops, List<OsmTag> Tags) GetOsmStopsList()
        {
            List<OsmStop> result = new List<OsmStop>();
            List<OsmTag> tags = new List<OsmTag>();

            XmlSerializer serializer =
                new XmlSerializer(typeof(Osm));

            using (Stream reader = new FileStream("Files/OsmStops.xml", FileMode.Open))
            {
                Osm osmRoot = (Osm)serializer.Deserialize(reader);

                foreach (Node node in osmRoot.Node)
                {
                    OsmStop stop = new OsmStop
                    {
                        Id = Guid.NewGuid(),
                        StopId = long.Parse(node.Id),
                        Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
                        Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
                    };

                    List<OsmTag> tempTags = new List<OsmTag>();

                    node.Tag.ForEach(x => tempTags.Add(new OsmTag()
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
    }
}
