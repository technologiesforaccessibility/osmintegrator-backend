// Testing Pull Request

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using Microsoft.AspNetCore.Authorization;
using OsmIntegrator.ApiModels.Errors;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using System.Linq;
using OsmIntegrator.Roles;
using OsmIntegrator.Database.Models;
using System.Transactions;
using OsmIntegrator.Tools;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ILogger<TagController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IValidationHelper _validationHelper;

        public TagController(
            ILogger<TagController> logger,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IValidationHelper validationHelper
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _validationHelper = validationHelper;
        }
 
       [HttpGet]
        public async Task<ActionResult<List<ApiModels.Tag>>> GetList()
        {
            try
            {
                var result = await _dbContext.Tags.ToListAsync();
                return Ok(_mapper.Map<List<ApiModels.Tag>>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unknown error while performing ");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("GetListForStop/{id}")]
        public async Task<ActionResult<List<ApiModels.Tag>>> GetListForStop(Guid id)
        {
            try
            {
                var result = await _dbContext.Tags.Where(x => x.StopId == id).ToListAsync();
                return Ok(_mapper.Map<List<ApiModels.Tag>>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unknown error while performing ");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("GetItem/{id}")]
        public async Task<ActionResult<ApiModels.Tag>> GetItem(Guid id)
        {
            try
            {
                var result = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id);
                return Ok(_mapper.Map<ApiModels.Tag>(result));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unknown error while performing ");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<ApiModels.Tag>> Delete(Guid id)
        {
            try
            {
                DbTag dbTag = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id);
                var result = _dbContext.Tags.Remove(dbTag);
                await _dbContext.SaveChangesAsync();
                return Ok("Tag deleted successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Problem with Tag validation.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Add([FromBody] ApiModels.Tag tag)
        {
            try
            {
                DbTag dbTag = _mapper.Map<DbTag>(tag);
                var result = await _dbContext.Tags.AddAsync(dbTag);
                await _dbContext.SaveChangesAsync();
                return Ok("Tag added successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Problem with Tag validation.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }


        [HttpPut]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Update([FromBody] ApiModels.Tag tag)
        {
            //var validationResult = _validationHelper.Validate(ModelState);
            //if (validationResult != null) return BadRequest(validationResult);

            try
            {
                Error error = await ValidateItem(tag);
                if (error != null)
                {
                    return BadRequest(error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unknown error while performing ");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }

            try
            {
                DbTag dbTag = _mapper.Map<DbTag>(tag);
                var result = _dbContext.Tags.Update(dbTag);
                _dbContext.SaveChanges();
                return Ok("Tag updated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Problem with Tag validation.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }           
        }

        private async Task<Error> ValidateItem(ApiModels.Tag tag)
        {
            Error result = null;
            return result;
        }
    }
}
