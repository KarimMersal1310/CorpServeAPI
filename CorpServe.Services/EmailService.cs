using CorpServe.Services.Abstraction;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var Email = _configuration["EmailSettings:Email"];
            var password = _configuration["EmailSettings:Password"];
            var host = _configuration["EmailSettings:Host"];
            var port = _configuration.GetValue<int>("EmailSettings:Port");

            if (string.IsNullOrWhiteSpace(Email))
                throw new InvalidOperationException("Missing email sender. Configure 'EmailSettings:Email'.");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Missing email password. Configure 'EmailSettings:Password'.");

            if (string.IsNullOrWhiteSpace(host) || port <= 0)
                throw new InvalidOperationException("Invalid SMTP settings. Configure 'EmailSettings:Host' and 'EmailSettings:Port'.");

            if (string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Recipient email address is required.");

            var smtpClient = new SmtpClient(host, port);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new System.Net.NetworkCredential(Email, password);

            var displayName = "CorpServe";
            var fromAddress = new MailAddress(Email, displayName);

            var message = new MailMessage();
            message.From = fromAddress;
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            await smtpClient.SendMailAsync(message);
        }
    }
}
