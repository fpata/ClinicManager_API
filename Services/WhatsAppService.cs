using System;
using System.Collections.Generic;
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

namespace ClinicManager.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly ClinicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public WhatsAppService(ClinicDbContext context, IConfiguration configuration, ILogger<WhatsAppService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendWhatsAppAsync(string toPhone, string message)
        {
            var enabledStr = _configuration["WhatsAppSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("WhatsApp service is disabled in configuration. Skipping WhatsApp sending.");
                return;
            }

            var provider = _configuration["WhatsAppSettings:Provider"] ?? "Mock";

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendWhatsAppViaTwilioAsync(toPhone, message);
            }
            else if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendWhatsAppViaMsg91Async(toPhone, message);
            }
            else
            {
                // Mock / Disabled
                _logger.LogWarning("------ MOCK WHATSAPP SENT (WHATSAPP IS CURRENTLY DISABLED) ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Message: {Message}", message);
                _logger.LogWarning("---------------------------------------------------------------");
                await Task.CompletedTask;
            }
        }

        private async Task SendWhatsAppViaTwilioAsync(string toPhone, string message)
        {
            // Try to load credentials from WhatsAppSettings, fallback to SmsSettings
            var accountSid = await GetSecretKeyAsync("WhatsAppSettings:Twilio:AccountSid", "WhatsAppSettings:Twilio:AccountSid");
            if (string.IsNullOrEmpty(accountSid))
            {
                accountSid = await GetSecretKeyAsync("SmsSettings:AccountSid", "SmsSettings:AccountSid");
            }

            var authToken = await GetSecretKeyAsync("WhatsAppSettings:Twilio:AuthToken", "WhatsAppSettings:Twilio:AuthToken");
            if (string.IsNullOrEmpty(authToken))
            {
                authToken = await GetSecretKeyAsync("SmsSettings:AuthToken", "SmsSettings:AuthToken");
            }

            var fromNumber = _configuration["WhatsAppSettings:Twilio:FromPhoneNumber"];
            if (string.IsNullOrEmpty(fromNumber)) fromNumber = _configuration["SmsSettings:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || accountSid.Contains("_PLACEHOLDER") ||
                string.IsNullOrEmpty(authToken) || authToken.Contains("_PLACEHOLDER") ||
                string.IsNullOrEmpty(fromNumber) || fromNumber.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("Twilio credentials are not fully configured for WhatsApp. Skipping message.");
                return;
            }

            // Twilio WhatsApp numbers must be prefixed with 'whatsapp:'
            var formattedTo = FormatTwilioWhatsAppNumber(toPhone);
            var formattedFrom = FormatTwilioWhatsAppNumber(fromNumber);

            var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            // Set Basic Auth
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            // Set urlencoded content
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("To", formattedTo),
                new KeyValuePair<string, string>("From", formattedFrom),
                new KeyValuePair<string, string>("Body", message)
            };
            request.Content = new FormUrlEncodedContent(postData);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Twilio WhatsApp API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"Twilio WhatsApp API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("WhatsApp message successfully sent to {ToPhone} via Twilio", toPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {ToPhone} via Twilio.", toPhone);
                throw;
            }
        }

        private string FormatTwilioWhatsAppNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return string.Empty;
            number = number.Trim();
            if (!number.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            {
                number = "whatsapp:" + number;
            }
            return number;
        }

        private async Task SendWhatsAppViaMsg91Async(string toPhone, string message)
        {
            var authKey = await GetSecretKeyAsync("WhatsAppSettings:Msg91:AuthKey", "WhatsAppSettings:Msg91:AuthKey");
            if (string.IsNullOrEmpty(authKey))
            {
                authKey = await GetSecretKeyAsync("Msg91Settings:AuthKey", "Msg91Settings:AuthKey");
            }

            var senderNumber = _configuration["WhatsAppSettings:Msg91:SenderNumber"];

            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured for WhatsApp. Skipping message.");
                return;
            }

            if (string.IsNullOrEmpty(senderNumber) || senderNumber.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 WhatsApp integrated_number is not configured. Skipping message.");
                return;
            }

            // MSG91 numbers must be cleaned (no whatsapp: prefix, no +, only numbers)
            string cleanRecipient = CleanPhoneNumber(toPhone);
            string cleanSender = CleanPhoneNumber(senderNumber);

            var payload = new
            {
                integrated_number = cleanSender,
                recipient_number = cleanRecipient,
                content_type = "text",
                text = message
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/whatsapp/whatsapp-outbound-message/");
            request.Headers.Add("authkey", authKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("MSG91 WhatsApp API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"MSG91 WhatsApp API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("WhatsApp message successfully sent via MSG91");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message via MSG91.");
                throw;
            }
        }

        private string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return string.Empty;
            
            // Remove 'whatsapp:' if present
            string cleaned = phone.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase);
            
            // Remove '+' and spaces
            cleaned = cleaned.Replace("+", "").Replace(" ", "").Trim();
            
            return cleaned;
        }

        public async Task SendTemplatedWhatsAppAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            var enabledStr = _configuration["WhatsAppSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("WhatsApp service is disabled in configuration. Skipping templated WhatsApp sending.");
                return;
            }

            var provider = _configuration["WhatsAppSettings:Provider"] ?? "Mock";

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendTemplatedWhatsAppViaTwilioAsync(toPhone, templateId, variables);
            }
            else if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendTemplatedWhatsAppViaMsg91Async(toPhone, templateId, variables);
            }
            else
            {
                // Mock / Disabled
                // Render templates locally for logging
                string rawTemplate = GetDefaultLocalTemplate(templateId);
                string message = ParseTemplate(rawTemplate, variables);

                _logger.LogWarning("------ MOCK TEMPLATED WHATSAPP SENT (WHATSAPP IS CURRENTLY DISABLED) ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Template ID: {TemplateId}", templateId);
                _logger.LogWarning("Rendered Message: {Message}", message);
                _logger.LogWarning("-------------------------------------------------------------------------");
                await Task.CompletedTask;
            }
        }

        private async Task SendTemplatedWhatsAppViaTwilioAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            // 1. Try to load template from database
            var dbTemplate = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.TemplateType == "WhatsApp" && t.IsActive == 1);
            
            string? templateContent = dbTemplate?.HtmlContent;

            // 2. Fall back to configuration
            if (string.IsNullOrEmpty(templateContent))
            {
                templateContent = _configuration[$"WhatsAppSettings:Templates:{templateId}"];
            }

            // 3. Fall back to default templates
            if (string.IsNullOrEmpty(templateContent))
            {
                templateContent = GetDefaultLocalTemplate(templateId);
            }

            // Populate default variables like user name and clinic name if they aren't provided
            await PopulateDefaultVariablesAsync(toPhone, variables);

            // Parse variables
            string message = ParseTemplate(templateContent, variables);

            await SendWhatsAppViaTwilioAsync(toPhone, message);
        }

        private async Task SendTemplatedWhatsAppViaMsg91Async(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            var authKey = await GetSecretKeyAsync("WhatsAppSettings:Msg91:AuthKey", "WhatsAppSettings:Msg91:AuthKey");
            if (string.IsNullOrEmpty(authKey))
            {
                authKey = await GetSecretKeyAsync("Msg91Settings:AuthKey", "Msg91Settings:AuthKey");
            }

            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured for WhatsApp. Skipping message.");
                return;
            }

            // Resolve MSG91 Flow ID from config mapping
            // e.g. WhatsAppSettings:Msg91:Templates:ForgotPassword
            var flowId = _configuration[$"WhatsAppSettings:Msg91:Templates:{templateId}"];
            if (string.IsNullOrEmpty(flowId))
            {
                // Fallback to direct templateId
                flowId = templateId;
            }

            await PopulateDefaultVariablesAsync(toPhone, variables);

            string cleanPhone = CleanPhoneNumber(toPhone);

            // MSG91 recipient dictionary requires "mobiles" and the rest of the dynamic variables
            var recipient = new Dictionary<string, string>
            {
                { "mobiles", cleanPhone }
            };
            foreach (var kvp in variables)
            {
                recipient[kvp.Key] = kvp.Value ?? string.Empty;
            }

            var payload = new
            {
                flow_id = flowId,
                recipients = new[] { recipient }
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/flow/");
            request.Headers.Add("authkey", authKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("MSG91 WhatsApp Flow API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"MSG91 WhatsApp Flow API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("WhatsApp message successfully sent via MSG91 Flow API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message via MSG91 Flow API.");
                throw;
            }
        }

        private async Task PopulateDefaultVariablesAsync(string toPhone, Dictionary<string, string> variables)
        {
            string cleanPhone = CleanPhoneNumber(toPhone);

            if (!variables.ContainsKey("user_name") && !variables.ContainsKey("UserName") && !variables.ContainsKey("user") && !variables.ContainsKey("PatientName"))
            {
                var user = await _context.Users
                    .Include(u => u.Contact)
                    .FirstOrDefaultAsync(u => u.Contact != null && 
                        (u.Contact.PrimaryPhone == toPhone || u.Contact.SecondaryPhone == toPhone || 
                         u.Contact.PrimaryPhone == "+" + cleanPhone || u.Contact.SecondaryPhone == "+" + cleanPhone) && u.IsActive == 1);
                
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

        private string GetDefaultLocalTemplate(string templateId)
        {
            return templateId switch
            {
                "ForgotPassword" => "Your verification code is: {otp}",
                "AppointmentCreated" => "Hi {user_name}, your appointment with {clinic_name} is scheduled for {Appointment_Date} at {Appointment_Time}.",
                "AppointmentReminder" => "Hi {user_name}, this is a reminder for your upcoming appointment with {clinic_name} on {Appointment_Date} at {Appointment_Time}.",
                _ => "Message: {message}"
            };
        }

        private string ParseTemplate(string template, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            foreach (var kvp in variables)
            {
                string key = kvp.Key;
                string value = kvp.Value ?? string.Empty;

                string patternDoubleBraces = @"\{\{\s*" + Regex.Escape(key) + @"\s*\}\}";
                string patternSingleBraces = @"\{\s*" + Regex.Escape(key) + @"\s*\}";

                template = Regex.Replace(template, patternDoubleBraces, value, RegexOptions.IgnoreCase);
                template = Regex.Replace(template, patternSingleBraces, value, RegexOptions.IgnoreCase);
            }

            return template;
        }

        private async Task<string?> GetSecretKeyAsync(string name, string configKey)
        {
            var dbKey = await _context.SystemKeys
                .FirstOrDefaultAsync(k => k.KeyName == name && k.IsActive == 1);
            
            if (dbKey != null)
            {
                return ClinicManager.Helpers.EncryptionHelper.Decrypt(dbKey.KeyValue);
            }

            return _configuration[configKey];
        }
    }
}
