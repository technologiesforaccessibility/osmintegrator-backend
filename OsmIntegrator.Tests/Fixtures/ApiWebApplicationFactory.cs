using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using osmintegrator;

namespace OsmIntegrator.Tests.Fixtures
{
  public class ApiWebApplicationFactory : WebApplicationFactory<Startup>
  {
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      // Set ASPNETCORE to Test
      // Thanks to this program will read appsettings.Test.json config file
      builder.UseEnvironment("Test");
    }
  }
}
