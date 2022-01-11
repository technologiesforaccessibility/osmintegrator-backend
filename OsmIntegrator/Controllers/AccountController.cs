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
using System.Threading.Tasks;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Tools;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using OsmIntegrator.Database.Models;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Roles;
using MimeKit;

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
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenHelper _tokenHelper;
    private readonly IStringLocalizer<AccountController> _localizer;
    private readonly IEmailHelper _emailHelper;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AccountController> logger,
        RoleManager<ApplicationRole> roleManager,
        ITokenHelper tokenHelper,
        IStringLocalizer<AccountController> localizer,
        IEmailHelper emailHelper)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
      _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
      _tokenHelper = tokenHelper ?? throw new ArgumentNullException(nameof(tokenHelper));
      _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
      _emailHelper = emailHelper ?? throw new ArgumentNullException(nameof(emailHelper));
    }

    [HttpGet]
    public IActionResult IsTokenValid()
    {
      return Ok(_localizer["Ok"]);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout(string returnUrl = null)
    {
      await _signInManager.SignOutAsync();

      if (returnUrl != null)
      {
        return LocalRedirect(returnUrl);
      }
      return Ok(_localizer["Ok"]);
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<TokenData>> Login([FromBody] LoginData model)
    {
      ApplicationUser userEmail = await _userManager.FindByEmailAsync(model.Email);

      if (userEmail == null)
      {
        // TODO: security vulnerability - disclosing users data
        throw new BadHttpRequestException(_localizer["Email doesn't exist"]);
      }

      var result = await _signInManager.PasswordSignInAsync(userEmail.UserName, model.Password, false, false);

      if (result.Succeeded)
      {
        var appUser = await _userManager.FindByEmailAsync(model.Email);
        List<string> userRoles = (List<string>)await _userManager.GetRolesAsync(appUser);

        TokenData tokenData = _tokenHelper.GenerateJwtToken(appUser.Id.ToString(), appUser, userRoles, _signInManager);
        return Ok(tokenData);
      }

      return Unauthorized(new AuthorizationError() { Message = _localizer["The username or password were not correct. Try again"] });
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
        return Unauthorized(new AuthorizationError() { Message = _localizer["Invalid refresh token"] });
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

      ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);

      if (user == null)
      {
        string errorMessage = _localizer["User with this email does not exist"];
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

      await _emailHelper.SendConfirmRegistrationMessageAsync(user);

      return Ok(_localizer["Your account has been activated"]);
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
          throw new BadHttpRequestException(_localizer["Email occupied"]);
        }
        RemoveUser(emailUser);
      }

      IdentityResult userAddedResult = await _userManager.CreateAsync(user, model.Password);

      if (!userAddedResult.Succeeded)
      {
        if (userAddedResult.Errors.First().Code == "DuplicateUserName")
        {
          throw new BadHttpRequestException(_localizer["User name has already been occupied"]);
        }
        _logger.LogError("Problem with creating user during registration: " +
          userAddedResult.Errors.FirstOrDefault().Code + " " +
          userAddedResult.Errors.FirstOrDefault().Description);
        throw new BadHttpRequestException(_localizer["An error occurred when registering the user"]);
      }

      await _userManager.AddToRoleAsync(user, UserRoles.EDITOR);

      if (bool.Parse(_configuration["RegisterConfirmationRequired"]))
      {
        await _emailHelper.SendRegisterMessageAsync(model, user);

        return Ok(_localizer["Confirmation email sent"]);
      }
      else
      {
        string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, token);

        return Ok(_localizer["User registered. No email confirmation required"]);
      }


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
      await _emailHelper.SendForgotPasswordMessageAsync(model.Email.Trim().ToLower());

      return Ok(_localizer["Reset password email has been sent"]);
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
        return Ok(_localizer["Email updated successfully!"]);
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
      await _emailHelper.SendChangeEmailMessageAsync(model.Email, User);

      return Ok(_localizer["Confirmation email sent"]);
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
    {
      ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);

      if (user != null)
      {
        IdentityResult result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (result.Succeeded)
        {
          return Ok(_localizer["Password reset successfully"]);
        }
        else
        {
          string errorMessage = string.Empty;
          foreach (var identityError in result.Errors)
          {
            errorMessage += identityError.Description;
          }

          throw new BadHttpRequestException(errorMessage);
        }
      }
      else
      {
        string errorMessage = _localizer["User with this email does not exist"];
        return Unauthorized(new AuthorizationError() { Message = errorMessage });
      }
    }
  }
}
