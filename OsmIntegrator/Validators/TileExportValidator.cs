using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database;

namespace OsmIntegrator.Validators
{
  public class TileExportValidator : ITileExportValidator
  {
    private IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public TileExportValidator(IConfiguration configuration, ApplicationDbContext dbContext)
    {
      _configuration = configuration;
      _dbContext = dbContext;
    }

    public async Task<bool> ValidateDelayAsync(Guid tileId)
    {
      DateTime? lastExportDate = await _dbContext.ExportReports
            .Where(r => r.TileId == tileId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.CreatedAt)
            .FirstOrDefaultAsync();

      var minExportDelayMins = byte.Parse(_configuration["OsmExportMinDelayMins"]);
      var exportUnlocksAt = lastExportDate?.AddMinutes(minExportDelayMins) ?? DateTime.MinValue;
      var minExportDelayExceeded = exportUnlocksAt < DateTime.Now;

      return minExportDelayExceeded;
    }
  }
}