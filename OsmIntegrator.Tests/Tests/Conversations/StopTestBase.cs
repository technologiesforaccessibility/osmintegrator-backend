using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Tests.Fixtures;

namespace OsmIntegrator.Tests.Tests.Conversations;

public class ConversationsTestBase : IntegrationTest
{
  public ConversationsTestBase(ApiWebApplicationFactory factory) : base(factory)
  {
    TestDataFolder = "Data/";
  }
}