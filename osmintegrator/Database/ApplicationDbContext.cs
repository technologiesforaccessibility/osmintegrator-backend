using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels;
using System.Collections.Generic;

namespace OsmIntegrator.Database
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private static IConfiguration _configuration;

        public DbSet<GtfsStop> GtfsStops { get; set; }

        public DbSet<OsmStop> OsmStops { get; set; }

        public DbSet<OsmTag> OsmTags { get; set; }

        public DbSet<LoginData> LoginDatas { get; set; }

        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
        }
        public static string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GtfsStop>().HasData(DataInitializer.GetGtfsStopsList());

            (List<OsmStop> Stops, List<OsmTag> Tags) = DataInitializer.GetOsmStopsList();

            modelBuilder.Entity<OsmStop>().HasData(Stops);
            modelBuilder.Entity<OsmTag>().HasData(Tags);

            base.OnModelCreating(modelBuilder);
        }
    }
}
