using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        private static readonly HttpClient _httpClient = new HttpClient();

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
            var enabledStr = _configuration["EmailSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("Email service is disabled in configuration. Skipping email sending.");
                return;
            }

            var provider = _configuration["EmailSettings:Provider"] ?? "SMTP";

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase) || 
                provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
            {
                await SendEmailViaSendGridAsync(toEmail, subject, body);
            }
            else if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendEmailViaMsg91Async(toEmail, subject, body);
            }
            else
            {
                // SMTP or Gmail
                await SendEmailViaSmtpAsync(toEmail, subject, body, provider);
            }
        }

        private async Task SendEmailViaSendGridAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["EmailSettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("Twilio/SendGrid API key is not configured. Skipping email sending.");
                return;
            }

            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = toEmail } }
                    }
                },
                from = new { email = senderEmail, name = senderName },
                subject = subject,
                content = new[]
                {
                    new { type = "text/html", value = body }
                }
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Twilio/SendGrid API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"Twilio/SendGrid API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("Email successfully sent to {ToEmail} via Twilio/SendGrid", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} via Twilio/SendGrid.", toEmail);
                throw;
            }
        }

        private async Task SendEmailViaMsg91Async(string toEmail, string subject, string body)
        {
            var authKey = _configuration["Msg91Settings:AuthKey"];
            var templateId = _configuration["Msg91Settings:Email:TemplateId"];

            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured. Skipping email sending.");
                return;
            }

            if (string.IsNullOrEmpty(templateId))
            {
                _logger.LogWarning("MSG91 Email TemplateId is not configured. Falling back to SMTP to send email.");
                await SendEmailViaSmtpAsync(toEmail, subject, body, "SMTP");
                return;
            }

            var senderEmail = _configuration["Msg91Settings:Email:SenderEmail"] ?? _configuration["EmailSettings:SenderEmail"] ?? "reliefdentalclinic52@gmail.com";
            var senderName = _configuration["Msg91Settings:Email:SenderName"] ?? _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";
            var domain = _configuration["Msg91Settings:Email:Domain"] ?? "gmail.com";

            var payload = new
            {
                recipients = new[]
                {
                    new
                    {
                        to = new[] { new { email = toEmail } },
                        variables = new Dictionary<string, string>
                        {
                            { "subject", subject },
                            { "body", body },
                            { "content", body }
                        }
                    }
                },
                from = new { name = senderName, email = senderEmail },
                domain = domain,
                template_id = templateId
            };

            await SendMsg91HttpRequestAsync(payload, authKey);
        }

        private async Task SendMsg91HttpRequestAsync(object payload, string authKey)
        {
            var jsonBody = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/email/send");
            request.Headers.Add("authkey", authKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("MSG91 Email API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"MSG91 Email API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("Email successfully sent via MSG91 API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via MSG91 API.");
                throw;
            }
        }

        private async Task SendEmailViaSmtpAsync(string toEmail, string subject, string body, string provider)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portStr = _configuration["EmailSettings:Port"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";
            var username = _configuration["EmailSettings:Username"];
            var rawPassword = _configuration["EmailSettings:Password"];
            var password = DecodeBase64Key(rawPassword);
            var enableSslStr = _configuration["EmailSettings:EnableSsl"] ?? "true";

            // If the provider is Gmail, we override server details to Gmail SMTP defaults
            if (provider.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
            {
                smtpServer = "smtp.gmail.com";
                portStr = "587";
                enableSslStr = "true";
            }

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
            var enabledStr = _configuration["EmailSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("Email service is disabled in configuration. Skipping templated email sending.");
                return;
            }

            var provider = _configuration["EmailSettings:Provider"] ?? "SMTP";

            if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendTemplatedEmailViaMsg91Async(toEmail, templateId, variables);
            }
            else
            {
                // For Twilio/SendGrid, Gmail, SMTP, we use the local DB template rendering and call SendEmailAsync
                await SendTemplatedEmailViaLocalRenderingAsync(toEmail, templateId, variables);
            }
        }

        private async Task SendTemplatedEmailViaMsg91Async(string toEmail, string templateId, Dictionary<string, string> variables)
        {
            var authKey = _configuration["Msg91Settings:AuthKey"];
            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured. Skipping email sending.");
                return;
            }

            // Resolve MSG91 Template ID from config mapping
            // e.g. Msg91Settings:Email:Templates:ForgotPassword
            var msg91TemplateId = _configuration[$"Msg91Settings:Email:Templates:{templateId}"];
            if (string.IsNullOrEmpty(msg91TemplateId))
            {
                // If not found in dictionary, try to use the templateId parameter directly as MSG91 Template ID
                msg91TemplateId = templateId;
            }

            var senderEmail = _configuration["Msg91Settings:Email:SenderEmail"] ?? _configuration["EmailSettings:SenderEmail"] ?? "reliefdentalclinic52@gmail.com";
            var senderName = _configuration["Msg91Settings:Email:SenderName"] ?? _configuration["EmailSettings:SenderName"] ?? "Relief Dental Clinic";
            var domain = _configuration["Msg91Settings:Email:Domain"] ?? "gmail.com";

            // Make sure variables has patient/user name and clinic name if they aren't provided
            await PopulateDefaultVariablesAsync(toEmail, variables);

            var payload = new
            {
                recipients = new[]
                {
                    new
                    {
                        to = new[] { new { email = toEmail } },
                        variables = variables
                    }
                },
                from = new { name = senderName, email = senderEmail },
                domain = domain,
                template_id = msg91TemplateId
            };

            await SendMsg91HttpRequestAsync(payload, authKey);
        }

        private async Task PopulateDefaultVariablesAsync(string toEmail, Dictionary<string, string> variables)
        {
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

            if (!variables.ContainsKey("clinic_name") && !variables.ContainsKey("ClinicName"))
            {
                var clinicConfig = await _context.AppConfigs
                    .FirstOrDefaultAsync(c => c.IsActive == 1);
                
                string activeClinicName = clinicConfig?.ClinicName ?? _configuration["EmailSettings:ClinicName"] ?? "Relief Dental Clinic";
                variables["clinic_name"] = activeClinicName;
            }
        }

        private async Task SendTemplatedEmailViaLocalRenderingAsync(string toEmail, string templateId, Dictionary<string, string> variables)
        {
            // 1. Fetch template from DB
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.TemplateType == "Email" && t.IsActive == 1);

            if (template == null)
            {
                _logger.LogError("Email template '{TemplateId}' not found in database.", templateId);
                throw new Exception($"Email template '{templateId}' not found in database.");
            }

            // 2. Populate default variables
            await PopulateDefaultVariablesAsync(toEmail, variables);

            // 3. Update and parse placeholders in subject and body
            string subject = template.Subject ?? string.Empty;
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
