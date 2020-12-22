using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using osmintegrator.Database;
using osmintegrator.Database.DataInitialization;
using osmintegrator.Models;
using System;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
        static private ServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();

            OnStartup();

            Console.ReadLine();

        }
        static private void ConfigureServices(ServiceCollection services)
        {
            //services.AddDbContext<ApplicationDbContext>(options =>
            //{
            //    options.UseNpgsql("User ID=osm_integrator;Password=super_compolicated_password_12345;Host=localhost;Port=5433;Database=osm_integrator;Pooling=true;");
            //});
            services.AddDbContext<ApplicationDbContext>();
            services.AddSingleton<DbSeeder>();
        }
        static void OnStartup()
        {
            var mainWindow = serviceProvider.GetService<DbSeeder>();
            mainWindow.Seed();
        }

        //public static IConfiguration GetConfiguration() =>
        //    new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.json", true, true)
        //        .Build();
    }
}
