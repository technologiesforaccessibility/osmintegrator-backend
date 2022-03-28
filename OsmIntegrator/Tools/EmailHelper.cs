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
    ApplicationUser applicationUser = await _userManager.GetUserAsync(user);

    string normalizedNewEmailAddress = newEmailAddress.Trim().ToLower();

    string token = await _userManager.GenerateChangeEmailTokenAsync(applicationUser, normalizedNewEmailAddress);

    string urlToResetPassword =
        _configuration["FrontendUrl"] + "/Account/ConfirmEmail?newEmail=" + normalizedNewEmailAddress + "&oldEmail=" + applicationUser.Email + "&token=" + token;

    MimeEntity messageBody = BuildChangeEmailMessageBody(normalizedNewEmailAddress, applicationUser.UserName, urlToResetPassword);
    string subject = GetSubject(_localizer["Confirm new email"]);

    await _emailService.SendEmailAsync(normalizedNewEmailAddress, subject, messageBody);
  }

  public async Task SendForgotPasswordMessageAsync(string userEmailAddress)
  {
    ApplicationUser user = await _userManager.FindByEmailAsync(userEmailAddress);

    if (user != null && await _userManager.IsEmailConfirmedAsync(user))
    {
      string token = await _userManager.GeneratePasswordResetTokenAsync(user);

      //Generate reset password link using url to frontend service, email and reset password token
      //for example:
      string urlToResetPassword = _configuration["FrontendUrl"] + "/Account/ResetPassword?email=" + userEmailAddress + "&token=" + token;
      // to do: create function to generate email message and subject
      // containing instruction what to do and url link to reset password

      string subject = GetSubject(_localizer["Resset password"]);
      MimeEntity messageBody = BuildForgotPasswordMessageBody(user.UserName, urlToResetPassword);

      await _emailService.SendEmailAsync(userEmailAddress, subject, messageBody);
    }
  }

  public async Task SendRegisterMessageAsync(RegisterData model, ApplicationUser user)
  {
    string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    string url = _configuration["FrontendUrl"] + "/Account/ConfirmRegistration?email=" + model.Email + "&token=" + token;

    string subject = GetSubject(_localizer["Confirm account registration"]);
    MimeEntity messageBody = BuildRegisterMessageBody(model.Username, url);

    await _emailService.SendEmailAsync(model.Email, subject, messageBody);
  }

  public async Task SendConfirmRegistrationMessageAsync(ApplicationUser user)
  {
    string subject = GetSubject(_localizer["Information for new users"]);
    MimeEntity messageBody = BuildConfirmRegistrationMessageBody(user.Email, user.UserName);

    await _emailService.SendEmailAsync(user.Email, subject, messageBody);
  }

  public async Task SendTileOccupiedMessageAsync(ApplicationUser user, string manualLink, long tileX, long tileY)
  {
    string subject = GetSubject(_localizer["Tile occupied for too long"]);
    MimeEntity messageBody = BuildSendTileOccupiedMessageBody(user.UserName, manualLink, tileX, tileY);

    await _emailService.SendEmailAsync(user.Email, subject, messageBody);
  }

  private MimeEntity BuildConfirmRegistrationMessageBody(string userEmailAddress, string username)
  {
    string slackDownload = _externalServicesConfiguration.SlackDownloadUrl;
    string userManualLink = _externalServicesConfiguration.UserManualUrl;
    string facebookGroupLink = _externalServicesConfiguration.FacebookGroupUrl;

    BodyBuilder builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["You have successfully created an account on"]} www.osmintegrator.pl. 
{_localizer["Next steps:"]}
{_localizer["Join our community on Slack. Write email on kontakt@rozwiazaniadlaniewidomych.org and we'll send you an invitation."]}
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
  <li>{_localizer["Join our community on Slack. Write email on kontakt@rozwiazaniadlaniewidomych.org and we'll send you an invitation."]}</li>
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
    BodyBuilder builder = new BodyBuilder();

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
    BodyBuilder builder = new BodyBuilder();

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
    BodyBuilder builder = new BodyBuilder();

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

  private MimeEntity BuildSendTileOccupiedMessageBody(string username, string manualLink, long tileX, long tileY)
  {
    BodyBuilder builder = new BodyBuilder();

    builder.TextBody = $@"{_localizer["Hello"]} {username},
{_localizer["The tile you have been working on has not yet been sent to"]} https://www.openstreetmap.org.
{_localizer["Please export your connections to allow other users to work in this area."]}
{_localizer["Read more about data synchronization in https://www.osmintegrator.eu in the manual at this link"]} {manualLink}.
{_localizer["Tile coordinates"]} X: {tileX}, Y: {tileY}
{GetServerName(false)}
{_localizer["Regards"]},
{_localizer["OsmIntegrator Team"]},
rozwiazaniadlaniewidomych.org
      ";

    builder.HtmlBody = $@"<h3>{_localizer["Hello"]} {username},</h3>
<p>{_localizer["The tile you have been working on has not yet been sent to"]} <a href=""https://www.openstreetmap.org"">OpenStreetMap</a>.</p><br/>
<p>{_localizer["Please export your connections to allow other users to work in this area."]}</p></br>
<p>{_localizer["Read more about data synchronization in"]} <a href=""https://www.osmintegrator.eu"">www.osmintegrator.eu</a> {_localizer["in the manual at"]} <a href=""{manualLink}"">{_localizer["this"]}</a> {_localizer["link"]}.</p></br>
<p>{_localizer["Tile coordinates"]} X: {tileX}, Y: {tileY}</p></br>
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