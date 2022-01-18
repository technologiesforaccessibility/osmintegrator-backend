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
using System.Transactions;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Database.Models;
using Microsoft.Extensions.Localization;

namespace OsmIntegrator.Controllers
{
    /// <summary>
    /// This controller allows to manage user roles.
    /// Make sure you've already read this article before making any changes in the code:
    /// https://github.com/technologiesforaccessibility/osmintegrator-wiki/wiki/Permissions-and-Roles
    /// </summary>
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class RolesController : ControllerBase
    {
        private readonly ILogger<RolesController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IMapper _mapper;
        private readonly IStringLocalizer<RolesController> _localizer;

        public RolesController(
            ILogger<RolesController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IStringLocalizer<RolesController> localizer
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
        public async Task<ActionResult<List<RoleUser>>> Get()
        {
            // Get current user roles
            var currentUser = await _userManager.GetUserAsync(User);
            List<string> currentUserRoles = (List<string>)await _userManager.GetRolesAsync(currentUser);

            // Get all users and remove current one
            List<ApplicationUser> allUsers = await _userManager.Users.ToListAsync();
            ApplicationUser userToRemove = allUsers.First(x => x.Email == currentUser.Email);
            allUsers.Remove(userToRemove);

            // Get all roles
            List<ApplicationRole> allRoles = await _roleManager.Roles.ToListAsync();

            // Assign roles to result users
            List<RoleUser> usersWithRoles = await GetUsersWithRoles(allUsers, allRoles);

            RemoveRoles(usersWithRoles, currentUserRoles);

            usersWithRoles.Sort((x, y) => x.UserName.CompareTo(y.UserName));

            return Ok(usersWithRoles);
        }

        [HttpGet("{role}/users")]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        public async Task<ActionResult<List<User>>> GetUsersWithRole(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role?.Trim().ToLower());      

            List<User> result = users
              .Select(user => new ApiModels.User()
              {
                UserName = user.UserName,
                Email = user.Email,
                Id = user.Id
              })
              .ToList();

          return Ok(result);
        }

        /// <summary>
        /// Remove roles to which current user has no permissions.
        /// </summary>
        private void RemoveRoles(List<RoleUser> usersWithRoles, List<string> currentUserRoles)
        {
            // The admin doesn't require to remove any roles.
            if (currentUserRoles.Contains(UserRoles.ADMIN)) return;

            foreach (RoleUser user in usersWithRoles)
            {
                user.Roles.RemoveAll(x => x.Name == UserRoles.UPLOADER);
                user.Roles.RemoveAll(x => x.Name == UserRoles.COORDINATOR);
                user.Roles.RemoveAll(x => x.Name == UserRoles.ADMIN);

                if (!currentUserRoles.Contains(UserRoles.COORDINATOR))
                {
                    user.Roles.RemoveAll(x => x.Name == UserRoles.SUPERVISOR);

                    if (!currentUserRoles.Contains(UserRoles.SUPERVISOR))
                    {
                        user.Roles.RemoveAll(x => x.Name == UserRoles.EDITOR);
                    }
                }
            }
        }

        private async Task<List<RoleUser>> GetUsersWithRoles(List<ApplicationUser> allUsers, List<ApplicationRole> allRoles)
        {
            List<RoleUser> usersWithRoles = new List<RoleUser>();
            allUsers.ForEach(x => usersWithRoles.Add(new RoleUser
            {
                Id = x.Id,
                UserName = x.UserName,
                Roles = new List<RolePair>()
            }));

            foreach (var role in allRoles)
            {
                List<ApplicationUser> usersInRole =
                    (List<ApplicationUser>)await _userManager.GetUsersInRoleAsync(role.Name);

                foreach (RoleUser user in usersWithRoles)
                {
                    if (usersInRole.Any(x => x.Id.Equals(user.Id)))
                    {
                        user.Roles.Add(new RolePair() { Name = role.Name, Value = true });
                        continue;
                    }
                    user.Roles.Add(new RolePair() { Name = role.Name, Value = false });
                }
            }

            return usersWithRoles;
        }

        [HttpPut]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Update([FromBody] List<RoleUser> users)
        {

            Error error = await ValidateRoles(users);
            if (error != null)
            {
                throw new BadHttpRequestException(_localizer["Roles validation failed"]);
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                List<string> errors = await UpdateRoles(users);

                if (errors.Count > 0)
                {
                    scope.Dispose();
                    throw new BadHttpRequestException(_localizer["Problem with update user roles"]);
                }
                scope.Complete();
                return Ok(_localizer["User roles updated successfully"]);
            }
        }

        /// <summary>
        /// Check if current user's roles allow to manage other user's roles.
        /// </summary>
        /// <param name="roleUsers">Users with updated roles.</param>
        /// <returns>Error if user doesn't have enough permissions otherwise null.</returns>
        private async Task<Error> ValidateRoles(List<RoleUser> roleUsers)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            List<string> currentUserRoles = (List<string>)await _userManager.GetRolesAsync(currentUser);

            // User can change any role
            if (currentUserRoles.Contains(UserRoles.ADMIN)) return null;

            List<string> errors = new List<string>();

            foreach (RoleUser user in roleUsers)
            {
                if (user.Roles.Any(x => x.Name == UserRoles.COORDINATOR))
                    errors.Add($"Current user do not have permission to edit {UserRoles.COORDINATOR} role for user: {user.UserName}.");
                if (user.Roles.Any(x => x.Name == UserRoles.UPLOADER))
                    errors.Add($"Current user do not have permission to edit {UserRoles.UPLOADER} role for user: {user.UserName}.");
                if (user.Roles.Any(x => x.Name == UserRoles.ADMIN))
                    errors.Add($"Current user do not have permission to edit {UserRoles.ADMIN} role for user: {user.UserName}.");
                if (!currentUserRoles.Contains(UserRoles.COORDINATOR))
                {
                    if (user.Roles.Any(x => x.Name == UserRoles.SUPERVISOR))
                        errors.Add($"Current user do not have permission to edit {UserRoles.SUPERVISOR} role for user: {user.UserName}.");

                    if (!currentUserRoles.Contains(UserRoles.SUPERVISOR))
                    {
                        if (user.Roles.Any(x => x.Name == UserRoles.EDITOR))
                            errors.Add($"Current user do not have permission to edit {UserRoles.EDITOR} role for user: {user.UserName}.");

                    }
                }
            }

            if (errors.Count > 0)
            {
                return new Error
                {
                    Title = _localizer["Role permissions problem"],
                    Message = string.Join(Environment.NewLine, errors)
                };
            }

            return null;
        }

        /// <summary>
        /// Add and remove roles received in parameter.
        /// </summary>
        /// <param name="users">List with new roles for users.</param>
        /// <returns>Collection with errors if any happened.</returns>
        private async Task<List<string>> UpdateRoles(List<RoleUser> users)
        {
            List<string> errors = new List<string>();
            foreach (RoleUser user in users)
            {
                ApplicationUser identityUser = await _userManager.FindByIdAsync(user.Id.ToString());
                IList<string> identityRoles = await _userManager.GetRolesAsync(identityUser);

                foreach (var rolePair in user.Roles)
                {
                    if (rolePair.Value)
                    {
                        if (!identityRoles.Contains(rolePair.Name))
                        {
                            IdentityResult result = await _userManager.AddToRoleAsync(identityUser, rolePair.Name);
                            if (!result.Succeeded)
                            {
                                errors.Add($"Unable to add role {rolePair.Name} to user {identityUser.UserName}.");
                            }
                        }
                    }
                    else
                    {
                        if (identityRoles.Contains(rolePair.Name))
                        {
                            IdentityResult result = await _userManager.RemoveFromRoleAsync(identityUser, rolePair.Name);
                            if (!result.Succeeded)
                            {
                                errors.Add($"Unable to remove role {rolePair.Name} from user {identityUser.UserName}.");
                            }
                        }
                    }
                }
            }
            return errors;
        }
    }
}