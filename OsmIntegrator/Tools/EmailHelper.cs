using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MimeKit;
using OsmIntegrator.ApiModels.Auth;
using OsmIntegrator.Controllers;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Interfaces;

public class EmailHelper : IEmailHelper
{
  private readonly IConfiguration _configuration;
  private readonly IStringLocalizer<AccountController> _localizer;
  private readonly IEmailService _emailService;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IExternalServicesConfiguration _externalServicesConfiguration;

  public EmailHelper(IConfiguration configuration, UserManager<ApplicationUser> userManager, IEmailService emailService, IStringLocalizer<AccountController> localizer, IExternalServicesConfiguration externalServicesConfiguration)
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    _externalServicesConfiguration = externalServicesConfiguration ?? throw new ArgumentNullException(nameof(externalServicesConfiguration));
  }

  public async Task SendChangeEmailMessageAsync(string newEmailAddress, ClaimsPrincipal user)
  {
    var applicationUser = await _userManager.GetUserAsync(user);

    var normalizedNewEmailAddress = newEmailAddress.Trim().ToLower();

    var token = await _userManager.GenerateChangeEmailTokenAsync(applicationUser, normalizedNewEmailAddress);

    var urlToResetPassword =
        _configuration["FrontendUrl"] + "/Account/ConfirmEmail?newEmail=" + normalizedNewEmailAddress + "&oldEmail=" + applicationUser.Email + "&token=" + token;

    var messageBody = BuildChangeEmailMessageBody(normalizedNewEmailAddress, applicationUser.UserName, urlToResetPassword);
    var subject = GetSubject(_localizer["Confirm new email"]);

    await _emailService.SendEmailAsync(normalizedNewEmailAddress, subject, messageBody);
  }

  public async Task SendForgotPasswordMessageAsync(string userEmailAddress)
  {
    var user = await _userManager.FindByEmailAsync(userEmailAddress);

    if (user != null && await _userManager.IsEmailConfirmedAsync(user))
    {
      var token = await _userManager.GeneratePasswordResetTokenAsync(user);

      //Generate reset password link using url to frontend service, email and reset password token
      //for example:
      var urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ResetPassword?email=" + userEmailAddress + "&token=" + token;
      // to do: create function to generate email message and subject
      // containing instruction what to do and url link to reset password

      var subject = GetSubject(_localizer["Resset password"]);
      var messageBody = BuildForgotPasswordMessageBody(user.UserName, urlToResetPassword);

      await _emailService.SendEmailAsync(userEmailAddress, subject, messageBody);
    }
  }

  public async Task SendRegisterMessageAsync(RegisterData model, ApplicationUser user)
  {
    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    var url = _configuration["FrontendUrl"] + "/Account/ConfirmRegistration?email=" + model.Email + "&token=" + token;

    var subject = GetSubject(_localizer["Confirm account registration"]);
    var messageBody = BuildRegisterMessageBody(model.Username, url);

    await _emailService.SendEmailAsync(model.Email, subject, messageBody);
  }

  public async Task SendConfirmRegistrationMessageAsync(ApplicationUser user)
  {
    var subject = GetSubject(_localizer["Information for new users"]);
    var messageBody = BuildConfirmRegistrationMessageBody(user.Email, user.UserName);

    await _emailService.SendEmailAsync(user.Email, subject, messageBody);
  }

  private MimeEntity BuildConfirmRegistrationMessageBody(string userEmailAddress, string username)
  {
    string slackInvitation = _externalServicesConfiguration.SlackInvitationUrl;
    string slackDownload = _externalServicesConfiguration.SlackDownloadUrl;
    string userManualLink = _externalServicesConfiguration.UserManualUrl;
    string facebookGroupLink = _externalServicesConfiguration.FacebookGroupUrl;

    var builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["You have successfully created an account on"]} www.osmintegrator.pl. 
{_localizer["Next steps:"]}
{_localizer["Join our community on Slack. Click on this link to create new account:"]} {slackInvitation}
{_localizer["Download Slack application and write welcome message at #general channel. We'll show you how to use the system. Link to download:"]} {slackDownload}
{_localizer["Read user manual available at this link:"]} {userManualLink}
{_localizer["Join our Facebook group:"]} {facebookGroupLink}
{GetServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
    builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {username},</h3>
<p>{_localizer["You have successfully created an account on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>.</p><br/>
<p>{_localizer["Next steps:"]}</p>
<ul>
  <li>{_localizer["Join our community on Slack. Click on this link to create new account:"]} <a href=""{slackInvitation}"">LINK</a></li>
  <li>{_localizer["Download Slack application and write welcome message at #general channel. We'll show you how to use the system. Link to download:"]} <a href=""{slackDownload}"">LINK</a></li>
  <li>{_localizer["Read user manual available at this link:"]} <a href=""{userManualLink}"">LINK</a></li>
  <li>{_localizer["Join our Facebook group:"]} <a href=""{facebookGroupLink}"">LINK</a></li>
</ul>
{GetServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>";

    return builder.ToMessageBody();
  }

  private MimeEntity BuildRegisterMessageBody(string username, string url)
  {
    var builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["You have just created an account on the site"]} www.osmintegrator.pl. {_localizer["To activate your account, click on the link below."]}
{url}
{GetServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
    builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {username},</h3>
<p>{_localizer["You have just created an account on the site"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To activate your account, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{GetServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>";

    return builder.ToMessageBody();
  }

  private MimeEntity BuildForgotPasswordMessageBody(string username, string url)
  {
    var builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["You have requested password reset on"]} www.osmintegrator.pl. {_localizer["To do so, click on the link below."]}
{url}
{GetServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
    builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {username},</h3>
<p>{_localizer["You have requested password reset on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To do so, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{GetServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>
      ";

    return builder.ToMessageBody();
  }

  private MimeEntity BuildChangeEmailMessageBody(string newEmailAddress, string username, string url)
  {
    var builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["You have requested changing an email address on"]} www.osmintegrator.pl. {_localizer["To do so, click on the link below."]}
{url}
{GetServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";
    builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {username},</h3>
<p>{_localizer["You have requested changing an email address on"]} <a href=""www.osmintegrator.pl"">www.osmintegrator.pl</a>. {_localizer["To do so, click on the link below."]}</p><br/>
<a href=""{url}"">{url}</a>
{GetServerName(true)}
<p>{_localizer["Regards"]},</p>
<p>{_localizer["OsmIntegrator Team"]},</p>
<a href=""rozwiazaniadlaniewidomych.org"">rozwiazaniadlaniewidomych.org</a>
      ";

    return builder.ToMessageBody();
  }

  private string GetSubject(string subject)
  {
    if (_configuration["FrontendUrl"].Contains("localhost"))
    {
      return $"[LOCAL] {subject}";
    }

    if (_configuration["FrontendUrl"] != "https://osmintegrator.eu")
    {
      return $"[TEST] {subject}";
    }

    return subject;
  }

  private string GetServerName(bool useHtml)
  {
    return _configuration["FrontendUrl"] != "https://osmintegrator.eu"
      ? (useHtml
        ? $"<p>Server: {_configuration["FrontendUrl"]}</p>"
        : "Server: " + _configuration["FrontendUrl"] + Environment.NewLine)
      : string.Empty;
  }
}