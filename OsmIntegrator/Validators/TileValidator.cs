using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.Validators
{
    public interface ITileValidator
    {
        Task<Error> Validate(ApplicationDbContext dbContext, string id);
    }

    public class TileValidator : ITileValidator
    {
        private readonly ILogger<TileValidator> _logger;

        public TileValidator(ILogger<TileValidator> logger)
        {
            _logger = logger;
        }
        public async Task<Error> Validate(ApplicationDbContext dbContext, string id)
        {
            try
            {

                if (string.IsNullOrEmpty(id))
                {
                    string message = $"Id cannot be null.";
                    return new ValidationError() { Message = message };
                }

                DbTile tile = await dbContext.Tiles
                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (tile == null)
                {
                    string message = $"No tile with id {id}.";
                    return new ValidationError() { Message = message };
                }

                return null;
            }
            catch (Exception e)
            {
                string message = $"Problem with validating tile with id {id}.";
                _logger.LogWarning(e, message);
                return new UnknownError() { Message = message };
            }
        }
    }
}