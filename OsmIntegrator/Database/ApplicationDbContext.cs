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

        public DbSet<Stop> Stops { get; set; }

        public DbSet<Tag> Tags { get; set; }

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
            List<Stop> allStops = _dataInitializer.GetGtfsStopsList();

            (List<Stop> Stops, List<Tag> Tags) = _dataInitializer.GetOsmStopsList();

            allStops.AddRange(Stops);

            List<Tile> tiles = _dataInitializer.GetTiles(allStops);

            modelBuilder.Entity<Stop>().HasData(allStops);
            modelBuilder.Entity<Tag>().HasData(Tags);
            modelBuilder.Entity<Tile>().HasData(tiles);

            base.OnModelCreating(modelBuilder);
        }
    }
}
