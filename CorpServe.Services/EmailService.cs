using CorpServe.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CorpServe.Services
{
    public class EmailService : IEmailService
    {
        private const string LogoContentId = "corpserve-logo";
        private const string LogoResourceName = "CorpServe.Services.EmailTemplates.corpserve-logo.png";
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

            var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
            var logoBytes = TryReadEmbeddedLogo();
            if (logoBytes is not null)
            {
                var logoStream = new MemoryStream(logoBytes);
                var logoResource = new LinkedResource(logoStream, "image/png")
                {
                    ContentId = LogoContentId,
                    TransferEncoding = TransferEncoding.Base64
                };

                htmlView.LinkedResources.Add(logoResource);
                message.AlternateViews.Add(htmlView);
                message.Body = string.Empty;
            }

            await smtpClient.SendMailAsync(message);
        }

        private static byte[]? TryReadEmbeddedLogo()
        {
            var assembly = typeof(EmailService).Assembly;
            using var stream = assembly.GetManifestResourceStream(LogoResourceName);
            if (stream is null)
                return null;

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
