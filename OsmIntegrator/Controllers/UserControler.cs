using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Roles;

namespace OsmIntegrator.Controllers
{

    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IMapper _mapper;
        private readonly IStringLocalizer<UserController> _localizer;

        public UserController(
            ILogger<UserController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<UserController> localizer
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<User>> Get()
        {

            var user = await _userManager.GetUserAsync(User);
            List<string> roles = (List<string>)await _userManager.GetRolesAsync(user);

            if (user == null)
            {
                throw new BadHttpRequestException(_localizer["Unable to find current user instance"]);
            }

            return Ok(new User()
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles
            });
        }

        [HttpGet("{id}")]
        [Authorize(UserRoles.SUPERVISOR + "," + UserRoles.COORDINATOR + ", " + UserRoles.ADMIN)]
        public async Task<ActionResult<User>> Get(string id)
        {

            if (string.IsNullOrEmpty(id))
            {
                throw new BadHttpRequestException(_localizer["Invalid id"]);
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                throw new BadHttpRequestException(_localizer["Unable to find current user instance"]);
            }

            List<string> roles = (List<string>)await _userManager.GetRolesAsync(user);

            if (roles == null)
            {
                throw new BadHttpRequestException(_localizer["Unable to find current user roles"]);
            }

            return Ok(new User()
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles
            });

        }
    }
}