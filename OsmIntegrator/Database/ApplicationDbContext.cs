using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private IConfiguration _configuration;

        public DbSet<DbStop> Stops { get; set; }

        public DbSet<DbTag> Tags { get; set; }

        public DbSet<DbTile> Tiles { get; set; }

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
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.USER, NormalizedName = OsmIntegrator.Roles.UserRoles.USER.ToUpper()});
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.EDITOR, NormalizedName = OsmIntegrator.Roles.UserRoles.EDITOR.ToUpper()});
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.SUPERVISOR, NormalizedName = OsmIntegrator.Roles.UserRoles.SUPERVISOR.ToUpper()});
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.COORDINATOR, NormalizedName = OsmIntegrator.Roles.UserRoles.COORDINATOR.ToUpper()});
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.UPLOADER, NormalizedName = OsmIntegrator.Roles.UserRoles.UPLOADER.ToUpper()});
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole { 
                Name = OsmIntegrator.Roles.UserRoles.ADMIN, NormalizedName = OsmIntegrator.Roles.UserRoles.ADMIN.ToUpper()});
            
            List<DbStop> allStops = _dataInitializer.GetGtfsStopsList();

            (List<DbStop> Stops, List<DbTag> Tags) = _dataInitializer.GetOsmStopsList();

            allStops.AddRange(Stops);

            List<DbTile> tiles = _dataInitializer.GetTiles(allStops);

            modelBuilder.Entity<DbStop>().HasData(allStops);
            modelBuilder.Entity<DbTag>().HasData(Tags);
            modelBuilder.Entity<DbTile>().HasData(tiles);

            base.OnModelCreating(modelBuilder);
        }
    }
}
