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
        private IConfiguration _configuration;

        public  DbSet<GtfsStop> GtfsStops { get; set; }

        public DbSet<OsmStop> OsmStops { get; set; }

        public DbSet<OsmTag> OsmTags { get; set; }

        public DbSet<LoginData> LoginDatas { get; set; }

        public DbSet<Tile> Tiles { get; set; }

        private DataInitializer _dataInitializer { get; set; }

        public ApplicationDbContext(IConfiguration configuration, DataInitializer dataInitializer)
        {
            _configuration = configuration;
            _dataInitializer = dataInitializer;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
        }
        public string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            List<GtfsStop> gtfsStops = _dataInitializer.GetGtfsStopsList();
            modelBuilder.Entity<GtfsStop>().HasData(gtfsStops);

            (List<OsmStop> Stops, List<OsmTag> Tags) = _dataInitializer.GetOsmStopsList();

            List<Tile> tiles = _dataInitializer.GetTiles(gtfsStops, Stops);

            modelBuilder.Entity<OsmStop>().HasData(Stops);
            modelBuilder.Entity<OsmTag>().HasData(Tags);

            base.OnModelCreating(modelBuilder);
        }
    }
}
