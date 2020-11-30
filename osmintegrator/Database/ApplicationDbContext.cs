using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace osmintegrator.Database
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private static IConfiguration _configuration;

        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
        }

        public static string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }
    }
}
