using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Interfaces;
using OsmIntegrator.ApiModels;
using System;
using System.Threading.Tasks;
using OsmIntegrator.Roles;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;
using OsmIntegrator.ApiModels.Errors;

namespace OsmIntegrator.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    public class PermissionsController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionsController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            RoleManager<IdentityRole> roleManager
            )
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var role = await _userManager.GetRolesAsync(user);
                return Ok(role);
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Description = ex.Message
                };
                return BadRequest(error);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddAdminRole()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (!await _roleManager.RoleExistsAsync(UserRoles.ADMIN))
                    {
                        var role = new IdentityRole();
                        role.Name = UserRoles.ADMIN;
                        await _roleManager.CreateAsync(role);
                        IdentityResult roleAddedResult = await _userManager.AddToRoleAsync(user, UserRoles.ADMIN);

                        if (!roleAddedResult.Succeeded)
                        {
                            Error error = new UnknownError();
                            error.Message = roleAddedResult.Errors.FirstOrDefault().Code + " " + roleAddedResult.Errors.FirstOrDefault().Description;
                            return BadRequest(error);
                        }
                    }

                return Ok();
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Description = ex.Message
                };
                return BadRequest(error);
            }
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> OnlyAdmin()
        {
            try
            {
                return await Task.Run(() => { return Ok("ok"); });
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Description = ex.Message
                };
                return BadRequest(error);
            }
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.ADMIN)]
        [Authorize(Roles = UserRoles.USER)]
        public async Task<IActionResult> AdminAndUser()
        {
            try
            {

                return await Task.Run(() => { return Ok("ok"); });
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Description = ex.Message
                };
                return BadRequest(error);
            }
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.USER)]
        public async Task<IActionResult> OnlyUser()
        {
            try
            {

                return await Task.Run(() => { return Ok("ok"); });
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Description = ex.Message
                };
                return BadRequest(error);
            }
        }
    }
}
