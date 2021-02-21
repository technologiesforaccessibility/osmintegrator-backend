using System;
using System.Collections.Generic;

namespace OsmIntegrator.ApiModels
{
    public class Tile
    {
        public Guid Id { get; set; }
        public long X { get; set; }
        public long Y { get; set; }
        public double MaxLat { get; set; }
        public double MinLon { get; set; }
        public double MinLat { get; set; }
        public double MaxLon { get; set; }
        public double OverlapMaxLat { get; set; }
        public double OverlapMinLon { get; set; }
        public double OverlapMinLat { get; set; }
        public double OverlapMaxLon { get; set; } = 0;
        public int OsmStopsCount { get; set; } = 0;
        public int GtfsStopsCount { get; set; }
        public int UsersCount { get; set; }
        public List<Stop> Stops { get; set; }
    }
}