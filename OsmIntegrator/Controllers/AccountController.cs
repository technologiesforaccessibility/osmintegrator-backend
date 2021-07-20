using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tools;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Validators;

namespace OsmIntegrator.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITokenHelper _tokenHelper;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            RoleManager<ApplicationRole> roleManager,
            ITokenHelper tokenHelper
            )
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _roleManager = roleManager;
            _tokenHelper = tokenHelper;
        }

        [HttpGet]
        public IActionResult IsTokenValid()
        {
            return Ok("Ok");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            return Ok("Ok");
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<TokenData>> Login([FromBody] LoginData model)
        {
                ApplicationUser userEmail = await _userManager.FindByEmailAsync(model.Email);

                if (userEmail == null)
                {
                    throw new BadHttpRequestException("Email doesn't exist");
                }

                var result = await _signInManager.PasswordSignInAsync(userEmail.UserName, model.Password, false, false);

                if (result.Succeeded)
                {
                    var appUser = await _userManager.FindByEmailAsync(model.Email);
                    List<string> userRoles = (List<string>)await _userManager.GetRolesAsync(appUser);

                    TokenData tokenData = _tokenHelper.GenerateJwtToken(appUser.Id.ToString(), appUser, userRoles, _signInManager);
                    return Ok(tokenData);
                }

                return Unauthorized(new AuthorizationError() { Message = "The username or password were not correct. Try again." });


        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<TokenData>> Refresh([FromBody] TokenData refreshTokenData)
        {

            var principal = await Task.Run(() =>
            {
                return _tokenHelper.GetPrincipalFromExpiredToken(refreshTokenData.Token);
            });

            var userId = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "sub").Value;
            var savedRefreshToken = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "refreshToken").Value;

            if (savedRefreshToken != refreshTokenData.RefreshToken)
            {
                return Unauthorized(new AuthorizationError() { Message = "Invalid refresh token" });
            }
            else
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Id == Guid.Parse(userId));

                List<string> roles = (List<string>)await _userManager.GetRolesAsync(appUser);
                TokenData tokenData = _tokenHelper.GenerateJwtToken(appUser.Id.ToString(), appUser, roles, _signInManager);
                return Ok(tokenData);
            }

        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistration model)
        {

            model.Email = model.Email.ToLower().Trim();

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                string errorMessage = "User with this email does not exist.";
                throw new BadHttpRequestException(errorMessage);
            }

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);

            if (!result.Succeeded)
            {
                string errorMessage = string.Empty;
                foreach (var identityError in result.Errors)
                {
                    errorMessage += identityError.Description;
                }

                throw new BadHttpRequestException(errorMessage);
            }
            return Ok("Registration confirmed.");

        }



        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Register([FromBody] RegisterData model)
        {
            ApplicationUser user = null;
            model.Email = model.Email.ToLower().Trim();
            model.Username = model.Username.Trim();

            user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = false
            };

            var emailUser = await _userManager.FindByEmailAsync(model.Email);
            if (emailUser != null)
            {
                if (emailUser.EmailConfirmed)
                {
                    throw new BadHttpRequestException("Email occupied");
                }
            }
            else
            {
                IdentityResult userAddedResult = await _userManager.CreateAsync(user, model.Password);

                if (!userAddedResult.Succeeded)
                {
                    RemoveUser(user);
                    throw new BadHttpRequestException(userAddedResult.Errors.FirstOrDefault().Code + " " + userAddedResult.Errors.FirstOrDefault().Description);
                }

                if (!bool.Parse(_configuration["RegisterConfirmationRequired"]))
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, token);
                    return Ok("User registered. No email confirmation required.");
                }
            }

            var t = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ConfirmRegistration?email=" + model.Email + "&token=" + t;
            _emailService.Send(model.Email, "Confirm account registration", "Click to confirm account registration:" + urlToResetPassword);
            return Ok("Confirmation email sent.");

        }


        private async void RemoveUser(ApplicationUser user)
        {
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {

            // var validationResult = _modelValidator.Validate(ModelState);
            // if (validationResult != null) return BadRequest(validationResult);
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                //Generate reset password link using url to frontend service, email and reset password token
                //for example:
                string urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ResetPassword?email=" + model.Email + "&token=" + token;
                // to do: create function to generate email message and subject
                // containing instruction what to do and url link to reset password

                _emailService.Send(model.Email, "Reset Password", "Click to reset password:" + urlToResetPassword);
                return Ok("Reset password email has been sent.");
            }
            else
            {
                return Unauthorized(new AuthorizationError() { Message = "User with this email does not exist or email was not confirmed." });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmail model)
        {
            model.NewEmail = model.NewEmail.ToLower().Trim();
            model.OldEmail = model.OldEmail.ToLower().Trim();

            var user = await _userManager.FindByEmailAsync(model.OldEmail);
            var result = await _userManager.ChangeEmailAsync(user, model.NewEmail, model.Token);
            if (result.Succeeded)
            {
                return Ok("Email updated successfully!");
            }

            string errorMessage = string.Empty;
            foreach (var identityError in result.Errors)
            {
                errorMessage += identityError.Description;
            }

            throw new BadHttpRequestException(errorMessage);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ChangeEmail([FromBody] ResetEmail model)
        {

            model.Email = model.Email.ToLower().Trim();

            var user = await _userManager.GetUserAsync(User);
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);

            string urlToResetPassword =
                _configuration["FrontendUrl"] + "/Account/ConfirmEmail?newEmail=" + model.Email + "&oldEmail=" + user.Email + "&token=" + token;

            _emailService.Send(model.Email, "Confirm email change", "Click to confirm new email:" + urlToResetPassword);
            return Ok("Confirmation email sent.");
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                if (result.Succeeded)
                {
                    return Ok("Password reset successfully.");
                }
                else
                {
                    string errorMessage = string.Empty;
                    foreach (var identityError in result.Errors)
                    {
                        errorMessage += identityError.Description;
                    }

                    return BadRequest(new Error() { Title = "Reset password failed.", Message = errorMessage });
                }
            }
            else
            {
                string errorMessage = "User with this email does not exist.";
                return Unauthorized(new AuthorizationError() { Message = errorMessage });
            }
        }
    }
}
