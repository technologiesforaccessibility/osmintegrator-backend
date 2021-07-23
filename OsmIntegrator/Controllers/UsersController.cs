using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Roles;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Database.Models;
using Microsoft.Extensions.Localization;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IMapper _mapper;
        private readonly IStringLocalizer<UsersController> _localizer;

        public UsersController(
            ILogger<UserController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IStringLocalizer<UsersController> localizer
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<List<User>>> Get()
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
                    Id = user.User.Id
                });
            }
            return Ok(result);
        }
    }
}