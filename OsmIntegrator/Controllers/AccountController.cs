﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OsmIntegrator.Interfaces;
using OsmIntegrator.ApiModels;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OsmIntegrator.Roles;

namespace OsmIntegrator.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
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
                    Description = e.Message
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
                    Description = e.Message
                };
                _logger.LogWarning(e, $"Unknown problem with {nameof(Logout)} method.");
                return BadRequest(error);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginData model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var serializableModelState = new SerializableError(ModelState);
                    return BadRequest(new ValidationError() { Message = JsonSerializer.Serialize(serializableModelState) });
                }

                IdentityUser userEmail = await _userManager.FindByEmailAsync(model.Email);

                if (userEmail == null)
                {
                    return BadRequest(new ValidationError() { Message = "Email doesn't exist" });
                }

                var result = await _signInManager.PasswordSignInAsync(userEmail.UserName, model.Password, false, false);

                if (result.Succeeded)
                {
                    var appUser = await _userManager.FindByEmailAsync(model.Email);
                    List<string> userRoles = (List<string>)await _userManager.GetRolesAsync(appUser);

                    TokenData tokenData = GenerateJwtToken(model.Email, appUser, userRoles);
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
        public async Task<IActionResult> Refresh([FromBody] TokenData refreshTokenData)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var serializableModelState = new SerializableError(ModelState);
                    return BadRequest(new ValidationError() { Message = JsonSerializer.Serialize(serializableModelState) });
                }

                var principal = await Task.Run(() => { return GetPrincipalFromExpiredToken(refreshTokenData.Token); });

                var username = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "sub").Value;
                var savedRefreshToken = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "refresh_token").Value;

                if (savedRefreshToken != refreshTokenData.RefreshToken)
                {
                    return Unauthorized(new AuthorizationError() { Message = "Invalid refresh token" });
                }
                else
                {
                    var appUser = _userManager.Users.SingleOrDefault(r => r.UserName == username);
                    List<string> roles = (List<string>)await _userManager.GetRolesAsync(appUser);
                    TokenData tokenData = GenerateJwtToken(username, appUser, roles);
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
        public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistration model)
        {
            try
            {
                model.Email = model.Email.ToLower().Trim();

                if (!ModelState.IsValid)
                {
                    Error error = new ValidationError();
                    var serializableModelState = new SerializableError(ModelState);
                    error.Message = JsonSerializer.Serialize(serializableModelState);
                    return BadRequest(error);
                }
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

                    return BadRequest(new Error() { Description = "Reset password failed", Message = errorMessage });
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ConfirmRegistration)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterData model)
        {
            IdentityUser user = null;
            try
            {
                model.Email = model.Email.ToLower().Trim();
                model.Username = model.Username.Trim();

                if (!ModelState.IsValid)
                {
                    Error error = new ValidationError();
                    var serializableModelState = new SerializableError(ModelState);
                    error.Message = JsonSerializer.Serialize(serializableModelState);
                    return BadRequest(error);
                }

                user = new IdentityUser
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

                    if (!await _roleManager.RoleExistsAsync(UserRoles.USER))
                    {
                        var role = new IdentityRole();
                        role.Name = UserRoles.USER;
                        await _roleManager.CreateAsync(role);
                        IdentityResult roleAddedResult = await _userManager.AddToRoleAsync(user, UserRoles.USER);

                        if (!roleAddedResult.Succeeded)
                        {
                            Error error = new UnknownError();
                            error.Message = roleAddedResult.Errors.FirstOrDefault().Code + " " + roleAddedResult.Errors.FirstOrDefault().Description;
                            RemoveUser(user);
                            return BadRequest(error);
                        }
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
                    _emailService.Send(model.Email, "Confirm account registration", "Click to confirm account registratino:" + urlToResetPassword);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to send email with confirmation token.");
                    return BadRequest(new Error()
                    {
                        Message = "Registration problem",
                        Description = "Unable to send email with confirmation token."
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

        private async void RemoveUser(IdentityUser user)
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

        private TokenData GenerateJwtToken(string userName, IdentityUser user, List<string> roles)
        {
            string newRefreshToken = GenerateRefreshToken();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("refresh_token", newRefreshToken)
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token");
            claimsIdentity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            ApplyClaimsForContextUser(claims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtExpireMinutes"]));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                claims: claimsIdentity.Claims,
                expires: expires,
                signingCredentials: creds
            );

            string result = (new JwtSecurityTokenHandler()).WriteToken(token);

            return new TokenData()
            {
                Token = result,
                RefreshToken = newRefreshToken
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = _configuration["JwtIssuer"],
                ValidAudience = _configuration["JwtIssuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"])),
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private void ApplyClaimsForContextUser(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims);
            _signInManager.Context.User = new ClaimsPrincipal(identity);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            try
            {
                if (ModelState.IsValid)
                {
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
                        return Ok();
                    }
                    else
                    {
                        return Unauthorized(new AuthorizationError() { Message = "User with this email does not exist or email was not confirmed." });
                    }
                }

                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);
                string errorMessage = string.Empty;
                foreach (var modalError in allErrors)
                {
                    errorMessage += modalError.ErrorMessage;
                }

                Error error = new Error() { Message = errorMessage };
                return BadRequest(error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ForgotPassword)} method.");
                Error error = new UnknownError() { Message = ex.Message };
                return BadRequest(error);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInformation()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if(user == null)
                {
                    return BadRequest("Unable to find current user instance");
                }

                return Ok(new UserInformation()
                {
                    UserName = user.UserName,
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(GetUserInformation)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmail model)
        {
            try
            {
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

                return BadRequest(new Error() { Description = "Email confirmation failed:", Message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unknown problem with {nameof(ConfirmEmail)} method.");
                return BadRequest(new UnknownError() { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeEmail([FromBody] ResetEmail model)
        {
            try
            {
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
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user != null)
                    {
                        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                        if (result.Succeeded)
                        {
                            return Ok();
                        }
                        else
                        {
                            string errorMessage = string.Empty;
                            foreach (var identityError in result.Errors)
                            {
                                errorMessage += identityError.Description;
                            }

                            return BadRequest(new Error() { Description = "Reset password failed", Message = errorMessage });
                        }
                    }
                    else
                    {
                        string errorMessage = "User with this email does not exist.";
                        return Unauthorized(new AuthorizationError() { Message = errorMessage });
                    }
                }
                else
                {
                    IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);
                    string errorMessage = string.Empty;
                    foreach (var modalError in allErrors)
                    {
                        errorMessage += modalError.ErrorMessage;
                    }

                    return BadRequest(new ValidationError() { Message = errorMessage });
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
