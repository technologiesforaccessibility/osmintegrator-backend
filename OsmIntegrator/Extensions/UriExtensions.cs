using System;

namespace OsmIntegrator.Extensions
{
  public static class UriExtensions
  {
    public static Uri UseRelativePath(this Uri item, string relativeUri, params object[] objs) => new Uri(item, string.Format(relativeUri, objs));
  }
}