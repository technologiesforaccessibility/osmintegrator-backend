using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ObjectsComparer;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;

namespace OsmIntegrator.Tests.Tests.OsmExports
{
  public class OsmExportTest : IntegrationTest
  {
    string expectedComment = "Updating ref and local_ref with GTFS data. " +
      "Tile X: 2264, Y: 1385, Zoom: 12. " +
      "Wiki: https://wiki.openstreetmap.org/w/index.php?title=Automated_edits/luktar/OsmIntegrator_-_fixing_stop_signs_for_blind";

    public OsmExportTest(ApiWebApplicationFactory factory) : base(factory)
    {
      TestDataFolder = "Data/OsmExports/";
    }

    [Theory]
    [InlineData("AddFieldsTest")]
    [InlineData("UpdateFieldsTest")]
    public async Task Test(string testName)
    {
      await InitTest(testName, "supervisor2", "supervisor1");

      DbStop gtfsStop = await _dbContext.Stops.FirstAsync(x => x.StopId == GTFS_STOP_ID_3);
      DbStop osmStop = await _dbContext.Stops.FirstAsync(x => x.StopId == OSM_STOP_ID_1);
      DbTile tile = _dbContext.Tiles.First(x => x.X == RIGHT_TILE_X && x.Y == RIGHT_TILE_Y);

      NewConnectionAction connectionAction = new()
      {
        TileId = tile.Id,
        OsmStopId = osmStop.Id,
        GtfsStopId = gtfsStop.Id
      };

      HttpResponseMessage response = await Put_Connections(connectionAction);
      response.StatusCode.Should().Be(HttpStatusCode.OK);

      OsmChange actualFile = await Get_OsmExport_GetChangeFile(tile.Id.ToString());
      OsmChangeOutput actualChanges = await Get_OsmExport_GetChanges(tile.Id.ToString());  

      OsmChange expectedChanges = SerializationHelper.XmlDeserializeFile<OsmChange>($"{TestDataFolder}{testName}/osmchange.xml");

      List<Difference> differences = Compare<OsmChange>(expectedChanges, actualFile);
      Assert.Empty(differences);
      Assert.Equal(expectedComment, actualChanges.Tags["comment"]);
    }
  }
}