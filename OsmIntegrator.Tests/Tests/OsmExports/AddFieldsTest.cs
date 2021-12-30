using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OsmIntegrator.ApiModels.Connections;
using OsmIntegrator.ApiModels.Reports;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Tests.Fixtures;
using OsmIntegrator.Tools;
using Xunit;

namespace OsmIntegrator.Tests.Tests.OsmExports
{
  public class AddFieldsTest : ExportsTestBase
  {
    public AddFieldsTest(ApiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Test()
    {
      await InitTest(nameof(AddFieldsTest), "supervisor2", "supervisor1");

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

      OsmChangeOutput output = await Get_OsmExport_GetChangeFile(tile.Id.ToString());

      File.WriteAllText("Output.txt", output.OsmChangeFileContent);

      OsmChange actual = SerializationHelper.XmlDeserialize<OsmChange>(output.OsmChangeFileContent);
    }
  }
}