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
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;

namespace OsmIntegrator.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly IMapper _mapper;

        public UsersController(
            ILogger<UserController> logger,
            IMapper mapper,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<User>>> Get()
        {
            try
            {
                var users = await _userManager.Users
                    .Select(u => new { User = u, Roles = new List<string>() })
                    .ToListAsync();

                var roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                foreach (var roleName in roleNames)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                    var toUpdate = users.Where(u => usersInRole.Any(ur => ur.Id == u.User.Id));
                    foreach (var user in toUpdate)
                    {
                        user.Roles.Add(roleName);
                    }
                }

                List<User> result = new List<User>();

                foreach (var user in users)
                {
                    result.Add(new ApiModels.User()
                    {
                        UserName = user.User.UserName,
                        Email = user.User.Email,
                        Roles = user.Roles,
                        Id = Guid.Parse(user.User.Id)
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }
    }
}