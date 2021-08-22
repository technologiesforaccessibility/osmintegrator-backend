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

        public DbSet<DbConnections> Connections { get; set; }

        public DbSet<DbNote> Notes { get; set; }

        private DataInitializer _dataInitializer { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbConnections>()
                .HasKey(t => new { t.Id });

            modelBuilder.Entity<DbConnections>()
                .HasOne(c => c.OsmStop)
                .WithMany(o => o.OsmConnections)
                .HasForeignKey(c => c.OsmStopId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DbConnections>()
                .HasOne(c => c.GtfsStop)
                .WithMany(o => o.GtfsConnections)
                .HasForeignKey(c => c.GtfsStopId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DbConnections>()
                .Property(x => x.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DbTile>()
                .HasOne(t => t.EditorApproved)
                .WithOne();

            modelBuilder.Entity<DbTile>()
                .HasOne(t => t.SupervisorApproved)
                .WithOne();

            modelBuilder.Entity<DbTile>()
                .HasMany(t => t.Users)
                .WithMany(u => u.Tiles)
                .UsingEntity(j => j.ToTable("ApplicationUserDbTile"));

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
