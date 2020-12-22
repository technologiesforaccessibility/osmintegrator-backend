using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using osmintegrator.Database.DataInitialization;
using osmintegrator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            //modelBuilder.Entity<Stop>().HasData(GetStopList());
            modelBuilder.Entity<Stop>().HasData(DataInitializer.GetZtmStopList("Files\\stops.txt"));
            base.OnModelCreating(modelBuilder);
        }
    }
}
