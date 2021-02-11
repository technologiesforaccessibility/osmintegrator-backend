using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Tools
{
	public class Point
    {
		public double X { get; set; }
		public double Y { get; set; }
    }

    public class TilesHelper
    {
		public static Point WorldToTilePos(double lon, double lat, int zoom)
		{
			Point result = new Point();
			result.X = (double)((lon + 180.0) / 360.0 * (1 << zoom));
			result.Y = (double)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
				1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

			return result;
		}

		public static Point TileToWorldPos(double x, double y, int zoom)
		{
			Point result = new Point();
			double n = Math.PI - ((2.0 * Math.PI * y) / Math.Pow(2.0, zoom));

			result.X = (double)((x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
			result.Y = (double)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

			return result;
		}
	}
}
