using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using osmintegrator.Database.DataInitialization;
using osmintegrator.Models;

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
        }
        public static string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stop>().HasData(DataInitializer.GetZtmStopList("Files/stops.txt"));
            base.OnModelCreating(modelBuilder);
        }
    }
}
