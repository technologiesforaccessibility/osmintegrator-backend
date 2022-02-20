using System;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
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

    public async Task SendEmailAsync(string to, string subject, MimeEntity body)
    {
        if (to.ToString().Contains("abcd.pl"))
        {
          _logger.LogDebug($"Omitting test email [sync] to '{to}' with subject '{subject}'" + Environment.NewLine + body);
          return;
        }

        _logger.LogDebug($"Sending email [sync] to '{to}' with subject '{subject}'" + Environment.NewLine + body);
        if (!bool.Parse(_configuration["SendEmails"]))
        {
          _logger.LogInformation($"Sending emails [sync] turned off. The email with subject '{subject}' will not be sent.");
          return;
        }

      try
      {
        MimeMessage email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = body;

        await SendEmailAsync(email);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email.");
      }
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
      try
      {
        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        await smtp.ConnectAsync(_configuration["Email:SmtpHost"], Convert.ToInt32(_configuration["Email:SmtpPort"]), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPass"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Unknown problem with sending email.");
      }
    }
  }
}
