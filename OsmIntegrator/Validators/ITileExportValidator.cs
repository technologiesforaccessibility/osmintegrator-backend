using System;
using System.Threading.Tasks;

namespace OsmIntegrator.Validators
{
  public interface ITileExportValidator
  {
    Task<bool> ValidateDelayAsync(Guid tileId);
  }
}