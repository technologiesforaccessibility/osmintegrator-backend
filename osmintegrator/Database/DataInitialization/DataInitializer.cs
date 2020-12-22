using osmintegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
            var customers = new List<Stop>
            {
                new Stop { StopId=2000, TypeId = 1, StopName = "Katowice, Kolista 2", Lat=59.345f, Lon=18.4353f},
                new Stop { StopId=2001, TypeId = 1, StopName = "Katowice, Lipowa 3", Lat=59.645f, Lon=18.8353f},
            };
            customers.ForEach(x => _context.Stops.Add(x));
            _context.SaveChanges();
        }

        /*
        public static void RecreateDatabase(AppDbContext context)
        {
            
            context.Database.EnsureDeleted();
            //context.Database.Migrate();
        }

        public static void ClearData(AppDbContext context)
        {
            ExecuteDeleteSql(context, "Orders");
            ExecuteDeleteSql(context, "Customers");
            ExecuteDeleteSql(context, "Inventory");
            ExecuteDeleteSql(context, "CreditRisks");
            ResetIdentity(context);
        }

        private static void ExecuteDeleteSql(AppDbContext context, string tableName)
        {
            //With 2.0, must separate string interpolation if not passing in params
            var rawSqlString = $"Delete from dbo.{tableName}";
            context.Database.ExecuteSqlCommand(rawSqlString);
        }

        private static void ResetIdentity(AppDbContext context)
        {
            var tables = new[] { "Inventory", "Orders", "Customers", "CreditRisks" };
            foreach (var itm in tables)
            {
                //With 2.0, must separate string interpolation if not passing in params
                var rawSqlString = $"DBCC CHECKIDENT (\"dbo.{itm}\", RESEED, -1);";
                context.Database.ExecuteSqlCommand(rawSqlString);
            }
        }
        */
    }
}
