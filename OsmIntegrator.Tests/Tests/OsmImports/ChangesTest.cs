using System.Linq;
using System.Threading.Tasks;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.OsmImports
{
  public class ChangesTest : ImportTestBase
  {
    public ChangesTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task ChangesWhileAddingStopTest()
    {
      await InitTest(nameof(AddStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      bool actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.True(actual);

      await Put_Tile_UpdateStops(tile.Id.ToString());

      actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.False(actual);
    }

    [Fact]
    public async Task ChangesWhileRemoveingStopTest()
    {
      await InitTest(nameof(RemoveStopTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      bool actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.True(actual);

      await Put_Tile_UpdateStops(tile.Id.ToString());

      actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.False(actual);
    }

    [Fact]
    public async Task ChangesWhileModifyingStopTest()
    {
      await InitTest(nameof(PositionTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      bool actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.True(actual);

      await Put_Tile_UpdateStops(tile.Id.ToString());

      actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.False(actual);
    }

    [Fact]
    public async Task ChangesWhileModifyingStopTagsTest()
    {
      await InitTest(nameof(TagsTest), "supervisor2", "supervisor1");

      DbTile tile = _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      bool actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.True(actual);

      await Put_Tile_UpdateStops(tile.Id.ToString());

      actual = await Get_Tile_ContainsChanges(tile.Id.ToString());

      Assert.False(actual);
    }
  }
}