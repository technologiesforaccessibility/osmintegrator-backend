using OsmIntegrator.ApiModels.Conversation;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.Conversations;

public class ChangeConversationPositionTest : ConversationsTestBase
{
  private const double EXPECTED_LAT = 50.231382;
  private const double EXPECTED_LON = 19.043227;

  public ChangeConversationPositionTest(ApiWebApplicationFactory fixture) : base(fixture)
  {

  }

  [Fact]
  public async Task ChangePositionTest()
  {
    await InitTest(nameof(ChangeConversationPositionTest), "editor1", "supervisor1");
    var c = _dbContext.Conversations.ToArray();
    DbConversation conversation = _dbContext.Conversations.First();

    ConversationPositionData data = new() { ConversationId = conversation.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };

    Conversation actualConversation = await ChangeConversationPosition(data);

    Assert.Equal(EXPECTED_LAT, actualConversation.Lat);
    Assert.Equal(EXPECTED_LON, actualConversation.Lon);
  }

}