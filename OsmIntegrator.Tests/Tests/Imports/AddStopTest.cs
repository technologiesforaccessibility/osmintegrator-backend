
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using Xunit;

namespace OsmIntegrator.Tests.Tests.Imports
{
  public class AddStopTest : ImportTestBase
  {
    public AddStopTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    // [Fact]
    // public async Task Test()
    // {
    //   await InitTest(nameof(TagsTest));


    // }
  }
}