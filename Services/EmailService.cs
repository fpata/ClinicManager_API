using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManager.DAL;
using ClinicManager.Models;

namespace ClinicManager.Services
{
    public class EmailService : IEmailService
    {
        private readonly ClinicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ClinicDbContext context, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        private string DecodeBase64Key(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            try
            {
                string paddedInput = input.Trim();
                int mod = paddedInput.Length % 4;
                if (mod == 2) paddedInput += "==";
                else if (mod == 3) paddedInput += "=";

                var bytes = Convert.FromBase64String(paddedInput);
                foreach (var b in bytes)
                {
                    if ((b < 32 && b != 9 && b != 10 && b != 13) || b >= 127)
                    {
                        return input;
                    }
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return input;
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portStr = _configuration["EmailSettings:Port"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";
            var username = _configuration["EmailSettings:Username"];
            // Password might be base64 encoded, let's check or decode base64 password just like credentials in connection string
            var rawPassword = _configuration["EmailSettings:Password"];
            var password = DecodeBase64Key(rawPassword);
            var enableSslStr = _configuration["EmailSettings:EnableSsl"] ?? "true";

           int port = 587;
            if (!int.TryParse(portStr, out port))
            {
                port = 587;
            }

            bool enableSsl = true;
            if (!bool.TryParse(enableSslStr, out enableSsl))
            {
                enableSsl = true;
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
                }

                _logger.LogInformation("Email successfully sent to {ToEmail} via SMTP Server: {SmtpServer}", toEmail, smtpServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP Server: {SmtpServer}.", toEmail, smtpServer);
                throw;
            }
        }

        public async Task SendTemplatedEmailAsync(string toEmail, string templateId, Dictionary<string, string> variables)
        {
            // 1. Fetch template from DB
            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.IsActive == 1);

            if (template == null)
            {
                _logger.LogError("Email template '{TemplateId}' not found in database.", templateId);
                throw new Exception($"Email template '{templateId}' not found in database.");
            }

            // 2. Fetch User to populate user name if not already in variables
            if (!variables.ContainsKey("user_name") && !variables.ContainsKey("UserName") && !variables.ContainsKey("user") && !variables.ContainsKey("PatientName"))
            {
                var user = await _context.Users
                    .Include(u => u.Contact)
                    .FirstOrDefaultAsync(u => u.Contact != null && (u.Contact.PrimaryEmail == toEmail || u.Contact.SecondaryEmail == toEmail) && u.IsActive == 1);
                
                if (user != null)
                {
                    variables["user_name"] = $"{user.FirstName} {user.LastName}";
                }
                else
                {
                    variables["user_name"] = "Valued Patient";
                }
            }

            // 3. Fetch Clinic Name to populate clinic_name if not already in variables
            if (!variables.ContainsKey("clinic_name") && !variables.ContainsKey("ClinicName"))
            {
                var clinicConfig = await _context.AppConfigs
                    .FirstOrDefaultAsync(c => c.IsActive == 1);
                
                string activeClinicName = clinicConfig?.ClinicName ?? _configuration["EmailSettings:ClinicName"] ?? "Relief Dental Clinic";
                variables["clinic_name"] = activeClinicName;
            }

            // 4. Update and parse placeholders in subject and body
            string subject = template.Subject;
            string htmlBody = template.HtmlContent;

            // Make a copy of keys to avoid modification exception
            var keys = new List<string>(variables.Keys);
            foreach (var key in keys)
            {
                string value = variables[key] ?? string.Empty;

                subject = ReplacePlaceholder(subject, key, value);
                htmlBody = ReplacePlaceholder(htmlBody, key, value);
            }

            // Also replace common appointment date/time aliases
            if (variables.TryGetValue("Appointment_Date", out var appointmentDate))
            {
                htmlBody = ReplacePlaceholder(htmlBody, "Appointment Date", appointmentDate);
                htmlBody = ReplacePlaceholder(htmlBody, "Appoint Date", appointmentDate);
                htmlBody = ReplacePlaceholder(htmlBody, "Appoint_Date", appointmentDate);
                
                subject = ReplacePlaceholder(subject, "Appointment Date", appointmentDate);
                subject = ReplacePlaceholder(subject, "Appoint Date", appointmentDate);
                subject = ReplacePlaceholder(subject, "Appoint_Date", appointmentDate);
            }

            if (variables.TryGetValue("Appointment_Time", out var appointmentTime))
            {
                htmlBody = ReplacePlaceholder(htmlBody, "Appointment Time", appointmentTime);
                htmlBody = ReplacePlaceholder(htmlBody, "Appointment_time", appointmentTime);
                htmlBody = ReplacePlaceholder(htmlBody, "Appointment time", appointmentTime);
                
                subject = ReplacePlaceholder(subject, "Appointment Time", appointmentTime);
                subject = ReplacePlaceholder(subject, "Appointment_time", appointmentTime);
                subject = ReplacePlaceholder(subject, "Appointment time", appointmentTime);
            }

            // Call SendEmailAsync with the populated content
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private string ReplacePlaceholder(string input, string key, string value)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Case-insensitive regex replacement for:
            // - {{key}}
            // - {key}
            string patternDoubleBraces = @"\{\{\s*" + Regex.Escape(key) + @"\s*\}\}";
            string patternSingleBraces = @"\{\s*" + Regex.Escape(key) + @"\s*\}";

            input = Regex.Replace(input, patternDoubleBraces, value, RegexOptions.IgnoreCase);
            input = Regex.Replace(input, patternSingleBraces, value, RegexOptions.IgnoreCase);

            return input;
        }
    }
}
