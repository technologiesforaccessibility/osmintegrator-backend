using osmintegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace osmintegrator.Database.DataInitialization
{
    public class DataInitializer
    {
        private ApplicationDbContext _context;

        public DataInitializer(ApplicationDbContext context)
        {
            _context = context;
        }
        public void InitializeData()
        {
            var stops = new List<Stop>
            {
                new Stop { StopId=2000, TypeId = 1, StopName = "Katowice, Kolista 2", Lat=59.345f, Lon=18.4353f},
                new Stop { StopId=2001, TypeId = 1, StopName = "Katowice, Lipowa 3", Lat=59.645f, Lon=18.8353f},
            };
            stops.ForEach(x => _context.Stops.Add(x));
            _context.SaveChanges();
        }

        public static List<Stop> GetZtmStopList(string fileName)
        {
            var csvStopList = CsvParser.Parse(fileName);
            var ztmStopList = csvStopList.Select((x, index) => new Stop()
            {
                StopId = index + 1,
                TypeId = 1,
                StopName = x[2],
                Lat = float.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
                Lon = float.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat)
            }).ToList();
            return ztmStopList;
        }
    }
}
