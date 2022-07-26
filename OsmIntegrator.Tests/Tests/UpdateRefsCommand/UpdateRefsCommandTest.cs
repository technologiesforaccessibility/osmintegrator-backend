using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ObjectsComparer;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.UpdateRefsCommand;

public class UpdateRefsCommandTest : IntegrationTest
{
  public UpdateRefsCommandTest(ApiWebApplicationFactory factory) : base(factory)
  {
    TestDataFolder = "Data/";
  }

  private async Task CreateConnection(long gtfsStopId, long osmStopId, Guid tileId)
  {
    DbStop gtfsStop = await _dbContext.Stops.FirstAsync(x => x.StopId == gtfsStopId);
    DbStop osmStop = await _dbContext.Stops.FirstAsync(x => x.StopId == osmStopId);

    NewConnectionAction connectionAction = new()
    {
      TileId = tileId,
      OsmStopId = osmStop.Id,
      GtfsStopId = gtfsStop.Id
    };

    HttpResponseMessage response = await Put_Connections(connectionAction);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task Test()
  {
    await InitTest(nameof(UpdateRefsCommandTest), "supervisor2", "supervisor1");

    DbTile rightTile =
      _dbContext.Tiles.First(x => x.X == ExpectedValues.RIGHT_TILE_X && x.Y == ExpectedValues.RIGHT_TILE_Y);
    await CreateConnection(ExpectedValues.GTFS_STOP_ID_3, ExpectedValues.OSM_STOP_ID_1, rightTile.Id);
    await CreateConnection(ExpectedValues.GTFS_STOP_ID_4, ExpectedValues.OSM_STOP_ID_2, rightTile.Id);
    await CreateConnection(ExpectedValues.GTFS_STOP_ID_5, ExpectedValues.OSM_STOP_ID_3, rightTile.Id);

    DbTile leftTile =
      _dbContext.Tiles.First(x => x.X == ExpectedValues.LEFT_TILE_X && x.Y == ExpectedValues.LEFT_TILE_Y);
    await CreateConnection(ExpectedValues.GTFS_STOP_ID_2, ExpectedValues.OSM_STOP_ID_4, leftTile.Id);
    await CreateConnection(ExpectedValues.GTFS_STOP_ID_1, ExpectedValues.OSM_STOP_ID_5, leftTile.Id);

    await _dbContext.Connections.ForEachAsync(x => x.Exported = true);
    await _dbContext.SaveChangesAsync();
    
    OsmChange actualFile = await Get_UpdateRefsCommand_GetChangeFile();
    actualFile.Mod.Nodes.Sort(
      (x, y) => string.Compare(x.Id, y.Id, StringComparison.Ordinal));
    actualFile.Mod.Nodes.ForEach(
      x => x.Tag.Sort((a, b) => string.Compare(a.K, b.K, StringComparison.Ordinal)));
    
    OsmChange expectedFile =
      SerializationHelper.XmlDeserializeFile<OsmChange>(
        $"{TestDataFolder}{nameof(UpdateRefsCommandTest)}/osmchange.xml");
    List<Difference> differences = Compare(expectedFile, actualFile);

    Assert.Empty(differences);
  }
}