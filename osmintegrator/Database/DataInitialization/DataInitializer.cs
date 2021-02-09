using OsmIntegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.DataInitialization
{
    public class DataInitializer
    {
        private ApplicationDbContext _context;

        public DataInitializer(ApplicationDbContext context)
        {
            _context = context;
        }

        public static List<Stop> GetGtfsStopList(string fileName)
        {
            List<string[]> csvStopList = CsvParser.Parse(fileName);
            List<Stop> ztmStopList = csvStopList.Select((x, index) => new Stop()
            {
                StopId = int.Parse(x[0]),
                Number = x[1],
                Name = x[2],
                StopType = StopType.Gtfs,
                Lat = float.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
                Lon = float.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat)
            }).ToList();
            return ztmStopList;
        }
    }
}
