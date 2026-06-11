using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Services
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

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portStr = _configuration["EmailSettings:Port"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var enableSslStr = _configuration["EmailSettings:EnableSsl"];

            // If not configured, mock send by logging
            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("------ MOCK EMAIL SENT ------");
                _logger.LogWarning("To: {ToEmail}", toEmail);
                _logger.LogWarning("Subject: {Subject}", subject);
                _logger.LogWarning("Body:\n{Body}", body);
                _logger.LogWarning("-----------------------------");
                return;
            }

            int port = 587;
            if (int.TryParse(portStr, out int p))
            {
                port = p;
            }

            bool enableSsl = true;
            if (bool.TryParse(enableSslStr, out bool ssl))
            {
                enableSsl = ssl;
            }

            try
            {
                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = enableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email successfully sent to {ToEmail}", toEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP.", toEmail);
                throw;
            }
        }
    }
}
