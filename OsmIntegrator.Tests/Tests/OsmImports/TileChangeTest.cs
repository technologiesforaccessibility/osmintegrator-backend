using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.OsmImports
{
  public class TileChangeTest : ImportTestBase
  {
    public TileChangeTest(ApiWebApplicationFactory fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(TileChangeTest), "supervisor2", "supervisor1");

      DbStop expectedStop1 = GetExpectedStop(ExpectedValues.OSM_STOP_ID_1, ExpectedValues.EXPECTED_LAT_OTHER_TILE, ExpectedValues.EXPECTED_LON_OTHER_TILE);

      DbTile tile = _dbContext.Tiles.AsNoTracking().First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
      Report report = await Put_Tile_UpdateStops(tile.Id.ToString());

      string actualTxtReport = report.Value;
      string expectedTxtReport =
        File.ReadAllText($"{TestDataFolder}{nameof(TileChangeTest)}/Report.txt");

      Assert.Equal(expectedTxtReport, actualTxtReport);

      DbTile targetTile = _dbContext.Tiles.AsNoTracking().First(x =>
        ExpectedValues.EXPECTED_LAT_OTHER_TILE >= x.MinLat && ExpectedValues.EXPECTED_LAT_OTHER_TILE <= x.MaxLat && ExpectedValues.EXPECTED_LON_OTHER_TILE >= x.MinLon && ExpectedValues.EXPECTED_LON_OTHER_TILE <= x.MaxLon);
      Report report1 = await Put_Tile_UpdateStops(targetTile.Id.ToString());

      string actualTxtReport1 = report1.Value;
      string expectedTxtReport1 =
        File.ReadAllText($"{TestDataFolder}{nameof(TileChangeTest)}/Report1.txt");

      Assert.Equal(expectedTxtReport1, actualTxtReport1);

      DbStop actualStop = _dbContext.Stops.AsNoTracking().First(x => x.StopId == ExpectedValues.OSM_STOP_ID_1);
      Assert.Equal(targetTile.Id, actualStop.TileId);
    }
  }
}