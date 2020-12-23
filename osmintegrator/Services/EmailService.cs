﻿using MailKit.Net.Smtp;
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

        public void Send(string from, string to, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();

            smtp.Connect(_configuration["SmtpHost"], Convert.ToInt32(_configuration["SmtpPort"]), SecureSocketOptions.StartTls);
            smtp.Authenticate(_configuration["SmtpUser"], _configuration["SmtpPass"]);
            smtp.Send(email);
            smtp.Disconnect(true);           
        }
    }
}
