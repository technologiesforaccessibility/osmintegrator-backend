using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using osmintegrator.Interfaces;
using System;

namespace osmintegrator.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Send(string to, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["Email:SmtpUser"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();

            smtp.Connect(_configuration["Email:SmtpHost"], Convert.ToInt32(_configuration["Email:SmtpPort"]), SecureSocketOptions.StartTls);
            smtp.Authenticate(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPass"]);
            smtp.Send(email);
            smtp.Disconnect(true);           
        }
    }
}
