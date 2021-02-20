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
using OsmIntegrator.Tools;
using System.Transactions;

namespace OsmIntegrator.Controllers
{
    /// <summary>
    /// This controller allows to manage user roles.
    /// Make sure you've already read this article before making any changes in the code:
    /// https://github.com/technologiesforaccessibility/osmintegrator-wiki/wiki/Permissions-and-Roles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly IValidationHelper _validationHelper;

        private readonly IMapper _mapper;

        public RolesController(
            ILogger<UserController> logger,
            IMapper mapper,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IValidationHelper validationHelper
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _validationHelper = validationHelper;
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
                // Get current user roles
                var currentUser = await _userManager.GetUserAsync(User);
                List<string> currentUserRoles = (List<string>)await _userManager.GetRolesAsync(currentUser);

                // Get all users and remove current one
                List<IdentityUser> allUsers = await _userManager.Users.ToListAsync();
                IdentityUser userToRemove = allUsers.First(x => x.Email == currentUser.Email);
                allUsers.Remove(userToRemove);

                // Get all roles
                List<IdentityRole> allRoles = await _roleManager.Roles.ToListAsync();

                // Assign roles to result users
                List<RoleUser> usersWithRoles = await GetUsersWithRoles(allUsers, allRoles);

                RemoveRoles(usersWithRoles, currentUserRoles);

                return Ok(usersWithRoles);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Get)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
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
                user.Roles.Remove(UserRoles.UPLOADER);
                user.Roles.Remove(UserRoles.COORDINATOR);
                user.Roles.Remove(UserRoles.ADMIN);

                if (!currentUserRoles.Contains(UserRoles.COORDINATOR))
                {
                    user.Roles.Remove(UserRoles.SUPERVISOR);

                    if (!currentUserRoles.Contains(UserRoles.SUPERVISOR))
                    {
                        user.Roles.Remove(UserRoles.EDITOR);
                    }
                }
            }
        }

        private async Task<List<RoleUser>> GetUsersWithRoles(List<IdentityUser> allUsers, List<IdentityRole> allRoles)
        {
            List<RoleUser> usersWithRoles = new List<RoleUser>();
            allUsers.ForEach(x => usersWithRoles.Add(new RoleUser
            {
                Id = x.Id,
                UserName = x.UserName,
                Roles = new Dictionary<string, bool>()
            }));

            foreach (var role in allRoles)
            {
                List<IdentityUser> usersInRole =
                    (List<IdentityUser>)await _userManager.GetUsersInRoleAsync(role.Name);

                foreach (RoleUser user in usersWithRoles)
                {
                    if (usersInRole.Any(x => x.Id.Equals(user.Id)))
                    {
                        user.Roles.Add(role.Name, true);
                        continue;
                    }
                    user.Roles.Add(role.Name, false);
                }
            }

            return usersWithRoles;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN + "," + UserRoles.COORDINATOR)]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] List<RoleUser> users)
        {
            var validationResult = _validationHelper.Validate(ModelState);
            if (validationResult != null) return BadRequest(validationResult);

            try
            {
                Error error = await ValidateRoles(users);
                if (error != null)
                {
                    return BadRequest(error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Problem with user roles validation.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    List<string> errors = await UpdateRoles(users);

                    if (errors.Count > 0)
                    {
                        scope.Dispose();
                        return BadRequest(new Error
                        {
                            Title = "Problem with adding/removing user roles.",
                            Message = string.Join($"{Environment.NewLine}", errors)
                        });
                    }
                    scope.Complete();
                    return Ok("User roles updated successfully!");
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    _logger.LogWarning(ex, $"Unknown problem with {nameof(Update)} method.");
                    return BadRequest(new UnknownError() { Message = ex.Message });
                }
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
                if (user.Roles.ContainsKey(UserRoles.COORDINATOR))
                    errors.Add($"Current user do not have permission to edit {UserRoles.COORDINATOR} role for user: {user.UserName}.");
                if (user.Roles.ContainsKey(UserRoles.UPLOADER))
                    errors.Add($"Current user do not have permission to edit {UserRoles.UPLOADER} role for user: {user.UserName}.");
                if (user.Roles.ContainsKey(UserRoles.ADMIN))
                    errors.Add($"Current user do not have permission to edit {UserRoles.ADMIN} role for user: {user.UserName}.");
                if (!currentUserRoles.Contains(UserRoles.COORDINATOR))
                {
                    if (user.Roles.ContainsKey(UserRoles.SUPERVISOR))
                        errors.Add($"Current user do not have permission to edit {UserRoles.SUPERVISOR} role for user: {user.UserName}.");

                    if (!currentUserRoles.Contains(UserRoles.SUPERVISOR))
                    {
                        if (user.Roles.ContainsKey(UserRoles.EDITOR))
                            errors.Add($"Current user do not have permission to edit {UserRoles.EDITOR} role for user: {user.UserName}.");

                    }
                }
            }

            if (errors.Count > 0)
            {
                return new Error
                {
                    Title = "Role permissions problem.",
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
                IdentityUser identityUser = await _userManager.FindByIdAsync(user.Id);
                IList<string> identityRoles = await _userManager.GetRolesAsync(identityUser);

                foreach (var rolePair in user.Roles)
                {
                    if (rolePair.Value)
                    {
                        if (!identityRoles.Contains(rolePair.Key))
                        {
                            IdentityResult result = await _userManager.AddToRoleAsync(identityUser, rolePair.Key);
                            if (!result.Succeeded)
                            {
                                errors.Add($"Unable to add role {rolePair.Key} to user {identityUser.UserName}.");
                            }
                        }
                    }
                    else
                    {
                        if (identityRoles.Contains(rolePair.Key))
                        {
                            IdentityResult result = await _userManager.RemoveFromRoleAsync(identityUser, rolePair.Key);
                            if (!result.Succeeded)
                            {
                                errors.Add($"Unable to remove role {rolePair.Key} from user {identityUser.UserName}.");
                            }
                        }
                    }
                }
            }
            return errors;
        }
    }
}