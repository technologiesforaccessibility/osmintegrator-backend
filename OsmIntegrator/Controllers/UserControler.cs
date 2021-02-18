using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Roles;

namespace OsmIntegrator.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IMapper _mapper;

        public UserController(
            ILogger<UserController> logger,
            IMapper mapper,
            UserManager<IdentityUser> userManager
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.USER)]
        public async Task<ActionResult<User>> Get()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                List<string> roles = (List<string>)await _userManager.GetRolesAsync(user);

                if (user == null)
                {
                    return BadRequest("Unable to find current user instance");
                }

                return Ok(new User()
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(string id)
        {
            try
            {
                if(string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ValidationError()
                    {
                        Message = $"Invalid id: {id}."
                    });
                }

                var user = await _userManager.FindByIdAsync(id);

                if(user == null)
                {
                    return BadRequest(new ValidationError()
                    {
                        Message = $"No user with id: {id}."
                    });
                }

                List<string> roles = (List<string>)await _userManager.GetRolesAsync(user);

                if (user == null)
                {
                    return BadRequest("Unable to find current user instance");
                }

                return Ok(new User()
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }
    }
}