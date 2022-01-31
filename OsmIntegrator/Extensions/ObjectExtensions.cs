using OsmIntegrator.Tools;

namespace OsmIntegrator.Extensions
{
  public static class ObjectExtensions
  {
    public static string ToXml<T>(this T item) where T : class => SerializationHelper.XmlSerialize(item);
  }
}