﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using osmintegrator.Database;
using osmintegrator.Database.DataInitialization;
using osmintegrator.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            // Application code should start here.

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(
                    (hostingContext, configuration) => {                        
                        IConfigurationRoot configurationRoot = configuration.Build();
                        }
                    )
                .ConfigureServices(
                    (_, services) => {
                        services
                            .AddDbContext<ApplicationDbContext>()
                            .AddHostedService<DbSeeder>();
                        }
                    );
    }
}
