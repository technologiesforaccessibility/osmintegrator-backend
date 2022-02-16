using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Tools;

namespace OsmIntegrator.Validators
{
  public class TileExportValidator : ITileExportValidator
  {
    private IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOverpass _overpass;
    private readonly IOsmUpdater _osmUpdater;
    private readonly IOsmExporter _osmExporter;

    public TileExportValidator(
      IConfiguration configuration,
      ApplicationDbContext dbContext,
      IOverpass overpass,
      IOsmUpdater osmUpdater,
      IOsmExporter osmExporter)
    {
      _configuration = configuration;
      _dbContext = dbContext;
      _overpass = overpass;
      _osmUpdater = osmUpdater;
      _osmExporter = osmExporter;
    }

    public async Task<bool> ValidateDelayAsync(Guid tileId)
    {
      DateTime? lastExportDate = await _dbContext.ExportReports
            .Where(r => r.TileId == tileId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.CreatedAt)
            .FirstOrDefaultAsync();

      byte minExportDelayMins = byte.Parse(_configuration["OsmExportMinDelayMins"]);
      DateTime exportUnlocksAt = lastExportDate?.AddMinutes(minExportDelayMins) ?? DateTime.MinValue;
      var minExportDelayExceeded = exportUnlocksAt.ToUniversalTime() < DateTime.Now.ToUniversalTime();

      return minExportDelayExceeded;
    }

    public async Task<bool> ValidateVersionAsync(Guid tileId)
    {
      DbTile tile = await _dbContext.Tiles
        .AsNoTracking()
        .Include(tile => tile.Stops)
        .FirstOrDefaultAsync(x => x.Id == tileId);

      Osm osm = await _overpass.GetArea(tile.MinLat, tile.MinLon, tile.MaxLat, tile.MaxLon);

      bool updatedValidated = !_osmUpdater.ContainsChanges(tile, osm);
      bool sameConnectionsValidated = !await _osmExporter.ContainsSameActiveConnections(tileId);

      return updatedValidated && sameConnectionsValidated;
    }
  }
}