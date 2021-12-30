using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace OsmIntegrator.Tools
{
  public class SerializationHelper
  {
    public static string XmlSerialize<T>(T changeNode)
    {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using StringWriter textWriter = new StringWriter();
      serializer.Serialize(textWriter, changeNode);
      return textWriter.ToString();
    }

    public static T XmlDeserialize<T>(string xml)
    {
      var serializer = new XmlSerializer(typeof(T));
      using TextReader reader = new StringReader(xml);
      return (T)serializer.Deserialize(reader);
    }

    public static T JsonDeserialize<T>(string fileName)
    {
      string file = File.ReadAllText(fileName);
      return JsonConvert.DeserializeObject<T>(file);
    }
  }
}