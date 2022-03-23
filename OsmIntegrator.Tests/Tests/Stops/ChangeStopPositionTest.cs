using OsmIntegrator.ApiModels.Stops;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Tests.Fixtures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using OsmIntegrator.Tests.Data;

namespace OsmIntegrator.Tests.Tests.Stops;

public class ChangeStopPositionTest : StopsTestBase
{
  private const double EXPECTED_LAT = 50.231382;
  private const double EXPECTED_LON = 18.982616;
  private const double EXPECTED_INIT_LAT = 50.231381;
  private const double EXPECTED_INIT_LON = 18.982615;

  public ChangeStopPositionTest(ApiWebApplicationFactory fixture)
    : base(fixture)
  {

  }

  [Fact]
  public async Task ChangePositionTest()
  {
    await InitTest(nameof(ChangeStopPositionTest), "editor1", "supervisor1");

    DbStop stop = _dbContext.Stops.First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);

    StopPositionData data = new() { StopId = stop.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };

    Stop actualStop = await ChangePosition(data);

    Assert.Equal(EXPECTED_LAT, actualStop.Lat);
    Assert.Equal(EXPECTED_LON, actualStop.Lon);
    Assert.Equal(EXPECTED_INIT_LAT, actualStop.InitLat);
    Assert.Equal(EXPECTED_INIT_LON, actualStop.InitLon);
  }

  [Fact]
  public async Task ChangePositionAllStopsTest()
  {
    await InitTest(nameof(ChangeStopPositionTest), "supervisor2", "supervisor1");

    DbStop stop = _dbContext.Stops.First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);

    StopPositionData data = new() { StopId = stop.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };

    await ChangePosition(data);

    List<Stop> actualStops = await GetAllStops();
    Stop actualStop = actualStops.First(x => x.Id == stop.Id);

    Assert.Equal(EXPECTED_LAT, actualStop.Lat);
    Assert.Equal(EXPECTED_LON, actualStop.Lon);
    Assert.Equal(EXPECTED_INIT_LAT, actualStop.InitLat);
    Assert.Equal(EXPECTED_INIT_LON, actualStop.InitLon);
  }

  [Fact]
  public async Task ResetPositionAllStopsTest()
  {
    await InitTest(nameof(ChangeStopPositionTest), "supervisor2", "supervisor1");

    DbStop stop = _dbContext.Stops.First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);

    StopPositionData data = new() { StopId = stop.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };

    await ChangePosition(data);
    await ResetPosition(stop.Id.ToString());

    List<Stop> actualStops = await GetAllStops();
    Stop actualStop = actualStops.First(x => x.Id == stop.Id);

    Assert.Equal(EXPECTED_INIT_LAT, actualStop.Lat);
    Assert.Equal(EXPECTED_INIT_LON, actualStop.Lon);
    Assert.Null(actualStop.InitLat);
    Assert.Null(actualStop.InitLon);
  }

  [Fact]
  public async Task ResetPositionTest()
  {
    await InitTest(nameof(ChangeStopPositionTest), "editor1", "supervisor1");

    DbStop stop = _dbContext.Stops.First(x => x.StopId == ExpectedValues.GTFS_STOP_ID_1);

    StopPositionData data = new() { StopId = stop.Id, Lat = EXPECTED_LAT, Lon = EXPECTED_LON };

    await ChangePosition(data);
    Stop actualStop = await ResetPosition(stop.Id.ToString());

    Assert.Equal(EXPECTED_INIT_LAT, actualStop.Lat);
    Assert.Equal(EXPECTED_INIT_LON, actualStop.Lon);
    Assert.Null(actualStop.InitLat);
    Assert.Null(actualStop.InitLon);
  }
}