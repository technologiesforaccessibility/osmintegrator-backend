using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using osmintegrator.Models;
using System.Collections.Generic;
using TS.Mobile.WebApp.Models;

namespace osmintegrator.Database
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private static IConfiguration _configuration;

        public DbSet<Stop> Stops { get; set; }
        public DbSet<LoginData> LoginDatas { get; set; }

        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
            //_ = optionsBuilder.UseNpgsql("User ID=osm_integrator;Password=super_compolicated_password_12345;Host=localhost;Port=5433;Database=osm_integrator;Pooling=true;");
        }
        public static string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stop>().HasData(GetEmployees());
            base.OnModelCreating(modelBuilder);
        }

        private List<Stop> GetEmployees()
        {
            return new List<Stop>
            {
                new Stop { StopId=2000, TypeId = 1, StopName = "Katowice, Kolista 2", Lat=59.345f, Lon=18.4353f},
                new Stop { StopId=2001, TypeId = 1, StopName = "Katowice, Lipowa 3", Lat=59.645f, Lon=18.8353f},
            };
        }
    }
}
