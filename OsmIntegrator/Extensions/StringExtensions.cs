using System;
using System.Text;

namespace OsmIntegrator.Extensions
{
  public static class StringExtensions
  {
    public static byte[] ToBytes(this string item) => Encoding.UTF8.GetBytes(item);
    public static Uri ToUri(this string item) => new Uri(item);
  }
}