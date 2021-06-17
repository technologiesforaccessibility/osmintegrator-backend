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
        private readonly IModelValidator _modelValidator;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            RoleManager<ApplicationRole> roleManager,
            IModelValidator validationHelper,
            ITokenHelper tokenHelper
            )
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _roleManager = roleManager;
            _modelValidator = validationHelper;
            _tokenHelper = tokenHelper;
        }

        [HttpGet]
        public IActionResult IsTokenValid()
        {
            try
            {
                return Ok("Ok");
            }
            catch (Exception e)
            {
                UnknownError error = new UnknownError()
                {
                    Title = e.Message
                };
                _logger.LogWarning(e, $"Unknown problem with {nameof(IsTokenValid)} method.");
                return BadRequest(error);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            try
            {
                await _signInManager.SignOutAsync();

                if (returnUrl != null)
                {
                    return LocalRedirect(returnUrl);
                }
                return Ok("Ok");
            }
            catch (Exception e)
            {
                UnknownError error = new UnknownError()
                {
                    Title = e.Message
                };
                _logger.LogWarning(e, $"Unknown problem with {nameof(Logout)} method.");
                return BadRequest(error);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<TokenData>> Login([FromBody] LoginData model)
        {
            try
            {
                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                ApplicationUser userEmail = await _userManager.FindByEmailAsync(model.Email);

                if (userEmail == null)
                {
                    return BadRequest(new ValidationError() { Message = "Email doesn't exist" });
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Login)} method.");

                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<TokenData>> Refresh([FromBody] TokenData refreshTokenData)
        {
            try
            {
                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                var principal = await Task.Run(() =>
                {
                    return _tokenHelper.GetPrincipalFromExpiredToken(refreshTokenData.Token);
                });

                var userId = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "sub").Value;
                var savedRefreshToken = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "refresh_token").Value;

                if (savedRefreshToken != refreshTokenData.RefreshToken)
                {
                    return Unauthorized(new AuthorizationError() { Message = "Invalid refresh token" });
                }
                else
                {
                    var appUser = _userManager.Users.SingleOrDefault(r => r.Id == long.Parse(userId));

                    List<string> roles = (List<string>)await _userManager.GetRolesAsync(appUser);
                    TokenData tokenData = _tokenHelper.GenerateJwtToken(appUser.Id.ToString(), appUser, roles, _signInManager);
                    return Ok(tokenData);
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Refresh)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistration model)
        {
            try
            {
                model.Email = model.Email.ToLower().Trim();

                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    string errorMessage = "User with this email does not exist.";
                    return BadRequest(new ValidationError() { Message = errorMessage });
                }

                var result = await _userManager.ConfirmEmailAsync(user, model.Token);

                if (!result.Succeeded)
                {
                    string errorMessage = string.Empty;
                    foreach (var identityError in result.Errors)
                    {
                        errorMessage += identityError.Description;
                    }

                    return BadRequest(new Error() { Title = "Reset password failed", Message = errorMessage });
                }
                return Ok("Registration confirmed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ConfirmRegistration)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }



        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Register([FromBody] RegisterData model)
        {
            ApplicationUser user = null;
            try
            {
                model.Email = model.Email.ToLower().Trim();
                model.Username = model.Username.Trim();

                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

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
                        Error error = new ValidationError();
                        error.Message = "Email occupied";
                        return BadRequest(error);
                    }
                }
                else
                {
                    IdentityResult userAddedResult = await _userManager.CreateAsync(user, model.Password);

                    if (!userAddedResult.Succeeded)
                    {
                        Error error = new UnknownError();
                        error.Message = userAddedResult.Errors.FirstOrDefault().Code + " " + userAddedResult.Errors.FirstOrDefault().Description;
                        RemoveUser(user);
                        return BadRequest(error);
                    }

                    if (!bool.Parse(_configuration["RegisterConfirmationRequired"]))
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        await _userManager.ConfirmEmailAsync(user, token);
                        return Ok("User registered. No email confirmation required.");
                    }
                }

                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    string urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ConfirmRegistration?email=" + model.Email + "&token=" + token;
                    _emailService.Send(model.Email, "Confirm account registration", "Click to confirm account registration:" + urlToResetPassword);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to send email with confirmation token.");
                    return BadRequest(new Error()
                    {
                        Message = "Registration problem",
                        Title = "Unable to send email with confirmation token."
                    });
                    throw;
                }

                return Ok("Confirmation email sent.");
            }
            catch (Exception ex)
            {

                RemoveUser(user);
                _logger.LogWarning(ex, $"Unknown problem with {nameof(Register)} method.");

                Error error = new UnknownError();
                error.Message = ex.Message;
                return BadRequest(error);
            }
        }


        private async void RemoveUser(ApplicationUser user)
        {
            if (user != null)
            {
                try
                {
                    await _userManager.DeleteAsync(user);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Unable to delete user {JsonSerializer.Serialize(user)}.");
                }
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            try
            {
                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ForgotPassword)} method.");
                Error error = new UnknownError() { Message = ex.Message };
                return BadRequest(error);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmail model)
        {
            try
            {
                model.NewEmail = model.NewEmail.ToLower().Trim();
                model.OldEmail = model.OldEmail.ToLower().Trim();

                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

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

                return BadRequest(new Error() { Title = "Email confirmation failed:", Message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ConfirmEmail)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ChangeEmail([FromBody] ResetEmail model)
        {
            try
            {
                model.Email = model.Email.ToLower().Trim();

                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                var user = await _userManager.GetUserAsync(User);
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);

                string urlToResetPassword =
                    _configuration["FrontendUrl"] + "/Account/ConfirmEmail?newEmail=" + model.Email + "&oldEmail=" + user.Email + "&token=" + token;

                _emailService.Send(model.Email, "Confirm email change", "Click to confirm new email:" + urlToResetPassword);
                return Ok("Confirmation email sent.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ChangeEmail)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            try
            {
                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ResetPassword)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }
    }
}
