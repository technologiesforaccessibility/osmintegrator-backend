using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace OsmIntegrator.Tools
{
  public class Utf8StringWriter : StringWriter
  {
    public override Encoding Encoding => Encoding.UTF8;
  }

  public class SerializationHelper
  {
    public static string XmlSerialize<T>(T changeNode)
    {
      XmlWriterSettings xmlSettings = new()
      {
        Indent = false,
        NewLineHandling = NewLineHandling.None,
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = true,
        NamespaceHandling = NamespaceHandling.OmitDuplicates
      };

      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using Utf8StringWriter textWriter = new();
      using XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlSettings);
      serializer.Serialize(xmlWriter, changeNode);
      return textWriter.ToString();
    }

    public static T XmlDeserializeFile<T>(string fileName)
    {
      string input = File.ReadAllText(fileName);
      return XmlDeserialize<T>(input);
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

    public static async Task<T> JsonDeserializeAsync<T>(HttpResponseMessage response)
    {
      string jsonResponse = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<T>(jsonResponse);
    }
  }
}