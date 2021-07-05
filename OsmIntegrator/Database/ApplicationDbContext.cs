using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using Microsoft.AspNetCore.Identity;
using System;

namespace OsmIntegrator.Database
{
    public class ApplicationDbContext :
        IdentityDbContext<
            ApplicationUser,
            ApplicationRole,
            Guid>
    {
        private IConfiguration _configuration;

        public DbSet<DbStop> Stops { get; set; }

        public DbSet<DbTile> Tiles { get; set; }

        public DbSet<DbStopLink> Connections { get; set; }

        public DbSet<DbNote> Notes { get; set; }

        private DataInitializer _dataInitializer { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Self many-to-many implemented thanks to this solution: 
            // https://stackoverflow.com/questions/49214748/many-to-many-self-referencing-relationship
            // Cascade delete was disabled: https://docs.microsoft.com/pl-pl/ef/core/saving/cascade-delete
            modelBuilder.Entity<DbStopLink>()
                .HasKey(t => new { t.Id });

            modelBuilder.Entity<DbStopLink>()
                .HasOne(c => c.OsmStop)
                .WithMany(o => o.StopLinks)
                .HasForeignKey(c => c.OsmStopId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DbStopLink>()
                .HasOne(c => c.GtfsStop)
                .WithMany()
                .HasForeignKey(c => c.GtfsStopId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DbStopLink>()
                .Property(x => x.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            base.OnModelCreating(modelBuilder);
        }

        public ApplicationDbContext(IConfiguration configuration, DataInitializer dataInitializer)
        {
            _configuration = configuration;
            _dataInitializer = dataInitializer;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
        }
        public string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }
    }
}
