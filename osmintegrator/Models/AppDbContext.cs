using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TS.Mobile.WebApp.Models;

namespace osmintegrator.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base()
        {

        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Stop> Stops { get; set; }
        public DbSet<LoginData> LoginDatas { get; set; }
    }
}
