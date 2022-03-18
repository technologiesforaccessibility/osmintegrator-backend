using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
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

    public DbSet<DbConnection> Connections { get; set; }

    public DbSet<DbConversation> Conversations { get; set; }

    public DbSet<DbMessage> Messages { get; set; }

    public DbSet<DbTileImportReport> ChangeReports { get; set; }

    public DbSet<DbTileExportReport> ExportReports { get; set; }

    public DbSet<DbGtfsImportReport> GtfsImportReports { get; set; }

    private DataInitializer _dataInitializer { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<DbConnection>()
          .HasKey(t => new { t.Id });

      modelBuilder.Entity<DbConnection>()
          .HasOne(c => c.OsmStop)
          .WithMany(o => o.OsmConnections)
          .HasForeignKey(c => c.OsmStopId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<DbConnection>()
          .HasOne(c => c.GtfsStop)
          .WithMany(o => o.GtfsConnections)
          .HasForeignKey(c => c.GtfsStopId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<DbConnection>()
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
      _ = optionsBuilder.UseNpgsql(_configuration["DBConnectionString"]);
      // optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information).EnableSensitiveDataLogging();
    }
  }
}
