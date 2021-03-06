﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Tools
{
	public class Point<T>
    {
		public T X { get; set; }
		public T Y { get; set; }

		public override bool Equals(object obj)
        {
            return obj is Point<T> element &&
                   X.Equals(element.X) &&
                   Y.Equals(element.Y);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }

    public class TilesHelper
    {
		public static Point<long> WorldToTilePos(double lon, double lat, int zoom)
		{
			Point<long> result = new Point<long>();
			result.X = (long)((lon + 180.0) / 360.0 * (1 << zoom));
			result.Y = (long)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
				1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
			return result;
		}

		public static Point<double> TileToWorldPos(long x, long y, int zoom)
		{
			Point<double> result = new Point<double>();
			double n = Math.PI - ((2.0 * Math.PI * y) / Math.Pow(2.0, zoom));
			result.X = (double)((x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
			result.Y = (double)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

			return result;
		}
	}
}
