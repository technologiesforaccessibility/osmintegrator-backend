using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using osmintegrator;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tests.Mocks;

namespace OsmIntegrator.Tests.Fixtures
{
  public class ApiWebApplicationFactory : WebApplicationFactory<Startup>
  {
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      // Set ASPNETCORE to Test
      // Thanks to this program will read appsettings.Test.json config file
      builder.UseEnvironment("Test");
      Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

      builder.ConfigureTestServices(services =>
      {
        var serviceProvider = services.BuildServiceProvider();
        var descriptor =
            new ServiceDescriptor(
                typeof(IOverpass),
                typeof(OverpassMock),
                ServiceLifetime.Singleton);
        services.Replace(descriptor);
      });
    }
  }
}
