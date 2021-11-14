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
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenHelper _tokenHelper;
    private readonly IStringLocalizer<AccountController> _localizer;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AccountController> logger,
        RoleManager<ApplicationRole> roleManager,
        ITokenHelper tokenHelper,
        IStringLocalizer<AccountController> localizer
        )
    {
      _logger = logger;
      _userManager = userManager;
      _signInManager = signInManager;
      _emailService = emailService;
      _configuration = configuration;
      _roleManager = roleManager;
      _tokenHelper = tokenHelper;
      _localizer = localizer;
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

      _emailService.Send(ConfirmRegistrationMessageBuilder(user));

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

      if (!bool.Parse(_configuration["RegisterConfirmationRequired"]))
      {
        string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, token);
        await _userManager.AddToRoleAsync(user, UserRoles.EDITOR);
        return Ok(_localizer["User registered. No email confirmation required"]);
      }

      string t = await _userManager.GenerateEmailConfirmationTokenAsync(user);

      _emailService.Send(RegisterMessageBuilder(model, _configuration["FrontendUrl"] + "/Account/ConfirmRegistration?email=" + model.Email + "&token=" + t));
      await _userManager.AddToRoleAsync(user, UserRoles.EDITOR);
      return Ok(_localizer["Confirmation email sent"]);
    }

    private MimeMessage ConfirmRegistrationMessageBuilder(ApplicationUser user)
    {
      string slackInvitation = "https://join.slack.com/t/techforaccessibility/shared_invite/zt-y7auomk3-iFugUKhN_Qz6_8Y37jySNw";
      string slackDownload = "https://slack.com/downloads";
      string userManualLink = "https://drive.google.com/file/d/14IfcF7DjK6fvD6V8GaOd7xAyUPWpCrZh/view?usp=sharing";
      string facebookGroupLink = "https://www.facebook.com/groups/282362010475827";

      MimeMessage message = new MimeMessage();
      message.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
      message.To.Add(MailboxAddress.Parse(user.Email));

      message.Subject = _emailService.BuildSubject(_localizer["Information for new users"]);

      BodyBuilder builder = new BodyBuilder();

      builder.TextBody = $@"{_localizer["Hello"]} {user.UserName},
{_localizer["You have successfully created an account on"]} www.osmintegrator.pl. 
{_localizer["Next steps:"]}
{_localizer["Join our community on Slack. Click on this link to create new account:"]} {slackInvitation}
{_localizer["Download Slack application and write welcome message at #general channel. We'll show you how to use the system. Link to download:"]} {slackDownload}
{_localizer["Read user manual available at this link:"]} {userManualLink}
{_localizer["Join our Facebook group:"]} {facebookGroupLink}
{_emailService.BuildServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
      builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {user.UserName},</h3>
<p>{_localizer["You have successfully created an account on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>.</p><br/>
<p>{_localizer["Next steps:"]}</p>
<ul>
  <li>{_localizer["Join our community on Slack. Click on this link to create new account:"]} <a href=""{slackInvitation}"">LINK</a></li>
  <li>{_localizer["Download Slack application and write welcome message at #general channel. We'll show you how to use the system. Link to download:"]} <a href=""{slackDownload}"">LINK</a></li>
  <li>{_localizer["Read user manual available at this link:"]} <a href=""{userManualLink}"">LINK</a></li>
  <li>{_localizer["Join our Facebook group:"]} <a href=""{facebookGroupLink}"">LINK</a></li>
</ul>
{_emailService.BuildServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>";

      message.Body = builder.ToMessageBody();

      return message;
    }

    private MimeMessage RegisterMessageBuilder(RegisterData model, string url)
    {
      MimeMessage message = new MimeMessage();
      message.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
      message.To.Add(MailboxAddress.Parse(model.Email));
      message.Subject = _emailService.BuildSubject(_localizer["Confirm account registration"]);

      BodyBuilder builder = new BodyBuilder();

      builder.TextBody = $@"{_localizer["Hello"]} {model.Username},
{_localizer["You have just created an account on the site"]} www.osmintegrator.pl. {_localizer["To activate your account, click on the link below."]}
{url}
{_emailService.BuildServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
      builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {model.Username},</h3>
<p>{_localizer["You have just created an account on the site"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To activate your account, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{_emailService.BuildServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>";

      message.Body = builder.ToMessageBody();

      return message;
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
      ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);

      if (user != null && await _userManager.IsEmailConfirmedAsync(user))
      {
        string token = await _userManager.GeneratePasswordResetTokenAsync(user);

        //Generate reset password link using url to frontend service, email and reset password token
        //for example:
        string urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ResetPassword?email=" + model.Email + "&token=" + token;
        // to do: create function to generate email message and subject
        // containing instruction what to do and url link to reset password

        _emailService.Send(ForgotPasswordMessageBuilder(model, user, urlToResetPassword));
        return Ok(_localizer["Reset password email has been sent"]);
      }
      else
      {
        return Unauthorized(new AuthorizationError() { Message = _localizer["User with this email does not exist or email was not confirmed"] });
      }
    }

    private MimeMessage ForgotPasswordMessageBuilder(ForgotPassword model, ApplicationUser user, string url)
    {
      MimeMessage message = new MimeMessage();
      message.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
      message.To.Add(MailboxAddress.Parse(model.Email));
      message.Subject = _emailService.BuildSubject(_localizer["Resset password"]);

      BodyBuilder builder = new BodyBuilder();

      builder.TextBody = $@"{_localizer["Hello"]} {user.UserName},
{_localizer["You have requested password reset on"]} www.osmintegrator.pl. {_localizer["To do so, click on the link below."]}
{url}
{_emailService.BuildServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
      builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {user.UserName},</h3>
<p>{_localizer["You have requested password reset on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To do so, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{_emailService.BuildServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>
      ";

      message.Body = builder.ToMessageBody();

      return message;
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

      model.Email = model.Email.ToLower().Trim();

      ApplicationUser user = await _userManager.GetUserAsync(User);
      string token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);

      string urlToResetPassword =
          _configuration["FrontendUrl"] + "/Account/ConfirmEmail?newEmail=" + model.Email + "&oldEmail=" + user.Email + "&token=" + token;

      _emailService.Send(ChangeEmailMessageBuilder(model, user, urlToResetPassword));
      return Ok(_localizer["Confirmation email sent"]);
    }

    private MimeMessage ChangeEmailMessageBuilder(ResetEmail model, ApplicationUser user, string url)
    {
      MimeMessage message = new MimeMessage();
      message.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
      message.To.Add(MailboxAddress.Parse(model.Email));
      message.Subject = _emailService.BuildSubject(_localizer["Confirm new email"]);

      BodyBuilder builder = new BodyBuilder();

      builder.TextBody = $@"{_localizer["Hello"]} {user.UserName},
{_localizer["You have requested changing an email address on"]} www.osmintegrator.pl. {_localizer["To do so, click on the link below."]}
{url}
{_emailService.BuildServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
      builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {user.UserName},</h3>
<p>{_localizer["You have requested changing an email address on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To do so, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{_emailService.BuildServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>
      ";

      message.Body = builder.ToMessageBody();

      return message;
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
