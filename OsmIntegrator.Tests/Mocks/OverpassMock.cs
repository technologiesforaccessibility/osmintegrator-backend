using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Tests.Mocks
{
  public class OverpassMock : IOverpass
  {
    public string OsmFileName { get; set; }
    private readonly DataInitializer _dataInitializer;

    public OverpassMock(DataInitializer dataInitializer)
    {
      _dataInitializer = dataInitializer;
    }
    
    public async Task<Osm> GetArea(double minLat, double minLong, double maxLat, double maxLong)
    {
      Osm osm = DeserializeFile(OsmFileName);

      List<Node> nodes = new();
      foreach (Node node in osm.Node)
      {
        if (double.Parse(node.Lat, CultureInfo.InvariantCulture) > minLat &&
            double.Parse(node.Lat, CultureInfo.InvariantCulture) <= maxLat &&
            double.Parse(node.Lon, CultureInfo.InvariantCulture) > minLong &&
            double.Parse(node.Lon, CultureInfo.InvariantCulture) <= maxLong)
        {
          nodes.Add(node);
        }
      }
      osm.Node = nodes;
      return osm;
    }

    public async Task<Osm> GetFullArea(ApplicationDbContext dbContext, CancellationToken cancelationToken)
    {
      return new Osm();
    }

    private Osm DeserializeFile(string fileName)
    {
      using FileStream reader = new(fileName, FileMode.Open);
      XmlSerializer serializer = new(typeof(Osm));
      return (Osm)serializer.Deserialize(reader);
    }
  }
}