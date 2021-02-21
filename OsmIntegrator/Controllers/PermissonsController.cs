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
using System.Net.Mime;
using System.Collections.Generic;
using OsmIntegrator.Database.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OsmIntegrator.Database;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]    
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    public class PermissionsController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _dbContext;

        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IMapper _mapper;

        public PermissionsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            RoleManager<ApplicationRole> roleManager,
            IMapper mapper,
            ApplicationDbContext dbContext
            )
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _roleManager = roleManager;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> Roles()
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
                    Title = ex.Message
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

                var role = new ApplicationRole();
                role.Name = UserRoles.ADMIN;
                await _roleManager.CreateAsync(role);
                IdentityResult roleAddedResult = await _userManager.AddToRoleAsync(user, UserRoles.ADMIN);

                if (!roleAddedResult.Succeeded)
                {
                    Error error = new UnknownError();
                    error.Message = roleAddedResult.Errors.FirstOrDefault().Code + " " + roleAddedResult.Errors.FirstOrDefault().Description;
                    return BadRequest(error);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                UnknownError error = new UnknownError()
                {
                    Title = ex.Message
                };
                return BadRequest(error);
            }
        }

        [HttpGet]
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
                    Title = ex.Message
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
                    Title = ex.Message
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
                    Title = ex.Message
                };
                return BadRequest(error);
            }
        }
    }
}
