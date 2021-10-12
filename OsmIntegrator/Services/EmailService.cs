using System;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Services
{
  public class EmailService : IEmailService
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    public void Send(string to, string subject, string message)
    {
      try
      {
        if (to.Contains("abcd.pl"))
        {
          _logger.LogDebug($"Omitting test email [sync] to '{to}' with subject '{subject}'" + Environment.NewLine + message);
          return;
        }

        _logger.LogDebug($"Sending email [sync] to '{to}' with subject '{subject}'" + Environment.NewLine + message);
        if (!bool.Parse(_configuration["SendEmails"]))
        {
          _logger.LogInformation($"Sending emails [sync] turned off. The email with subject '{subject}' will not be sent.");
          return;
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = message };

        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        smtp.Connect(_configuration["Email:SmtpHost"], Convert.ToInt32(_configuration["Email:SmtpPort"]), SecureSocketOptions.StartTls);
        smtp.Authenticate(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPass"]);
        smtp.Send(email);
        smtp.Disconnect(true);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email.");
      }
    }

    public void Send(MimeMessage message)
    {
      try
      {
        if (message.To.ToString().Contains("abcd.pl"))
        {
          _logger.LogDebug($"Omitting test email [sync] to '{message.To}' with subject '{message.Subject}'" + Environment.NewLine + message.TextBody);
          return;
        }

        _logger.LogDebug($"Sending email [sync] to '{message.To}' with subject '{message.Subject}'" + Environment.NewLine + message.TextBody);
        if (!bool.Parse(_configuration["SendEmails"]))
        {
          _logger.LogInformation($"Sending emails [sync] turned off. The email with subject '{message.Subject}' will not be sent.");
          return;
        }

        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        smtp.Connect(_configuration["Email:SmtpHost"], Convert.ToInt32(_configuration["Email:SmtpPort"]), SecureSocketOptions.StartTls);
        smtp.Authenticate(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPass"]);
        smtp.Send(message);
        smtp.Disconnect(true);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email.");
      }
    }

    public async Task SendEmailAsync(string to, string subject, string message)
    {
      try
      {
        if (to.Contains("abcd.pl"))
        {
          _logger.LogDebug($"Omitting test email [async] to '{to}' with subject '{subject}'" + Environment.NewLine + message);
          return;
        }
        _logger.LogDebug($"Sending email [async] to '{to}' with subject '{subject}'" + Environment.NewLine + message);
        if (!bool.Parse(_configuration["SendEmails"]))
        {
          _logger.LogInformation($"Sending emails [async] turned off. The email with subject '{subject}' will not be sent.");
          return;
        }
        await Task.Run(() =>
        {
          Send(to, subject, message);
        }
        ).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email async.");
      }
    }

        public async Task SendEmailAsync(MimeMessage message)
    {
      try
      {
        if (message.To.ToString().Contains("abcd.pl"))
        {
          _logger.LogDebug($"Omitting test email [async] to '{message.To}' with subject '{message.Subject}'" + Environment.NewLine + message.TextBody);
          return;
        }
        _logger.LogDebug($"Sending email [async] to '{message.To}' with subject '{message.Subject}'" + Environment.NewLine + message);
        if (!bool.Parse(_configuration["SendEmails"]))
        {
          _logger.LogInformation($"Sending emails [async] turned off. The email with subject '{message.Subject}' will not be sent.");
          return;
        }
        await Task.Run(() =>
        {
          Send(message);
        }
        ).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email async.");
      }
    }
  }
}
