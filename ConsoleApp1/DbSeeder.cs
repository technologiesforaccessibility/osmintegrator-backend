using osmintegrator.Database;
using osmintegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class DbSeeder
    {
        private readonly ApplicationDbContext _dbContext;

        public DbSeeder(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            Seed();
        }

        public void Seed()
        {
            _dbContext.Stops.AddRange(
                new List<Stop>
                {
                new Stop { StopId=3000, TypeId = 1, StopName = "Katowice, Kolista 2", Lat=59.345f, Lon=18.4353f},
                new Stop { StopId=3001, TypeId = 1, StopName = "Katowice, Lipowa 3", Lat=59.645f, Lon=18.8353f},
                }
            );
        }
    }
}
