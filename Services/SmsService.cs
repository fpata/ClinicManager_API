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
    public class SmsService : ISmsService
    {
        private readonly ClinicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public SmsService(ClinicDbContext context, IConfiguration configuration, ILogger<SmsService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            var enabledStr = _configuration["SmsSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("SMS service is disabled in configuration. Skipping SMS sending.");
                return;
            }

            var provider = _configuration["SmsSettings:Provider"] ?? "Mock";

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendSmsViaTwilioAsync(toPhone, message);
            }
            else if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendSmsViaMsg91Async(toPhone, message);
            }
            else
            {
                // Mock / Disabled
                _logger.LogWarning("------ MOCK SMS SENT (SMS IS CURRENTLY DISABLED) ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Message: {Message}", message);
                _logger.LogWarning("-----------------------------------------------------");
                await Task.CompletedTask;
            }
        }

        private async Task SendSmsViaTwilioAsync(string toPhone, string message)
        {
            var accountSid = await GetSecretKeyAsync("SmsSettings:AccountSid", "SmsSettings:AccountSid");
            var authToken = await GetSecretKeyAsync("SmsSettings:AuthToken", "SmsSettings:AuthToken");
            var fromNumber = _configuration["SmsSettings:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || accountSid.Contains("_PLACEHOLDER") ||
                string.IsNullOrEmpty(authToken) || authToken.Contains("_PLACEHOLDER") ||
                string.IsNullOrEmpty(fromNumber) || fromNumber.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("Twilio SMS credentials are not fully configured. Skipping SMS sending.");
                return;
            }

            // Create HTTP request to Twilio API
            var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            // Set Basic Auth
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            // Set urlencoded content
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("To", toPhone),
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("Body", message)
            };
            request.Content = new FormUrlEncodedContent(postData);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Twilio SMS API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"Twilio SMS API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("SMS successfully sent to {ToPhone} via Twilio", toPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {ToPhone} via Twilio.", toPhone);
                throw;
            }
        }

        private async Task SendSmsViaMsg91Async(string toPhone, string message)
        {
            var authKey = await GetSecretKeyAsync("Msg91Settings:AuthKey", "Msg91Settings:AuthKey");
            var templateId = _configuration["Msg91Settings:Sms:TemplateId"];
            var variableName = _configuration["Msg91Settings:Sms:VariableName"] ?? "message";

            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured. Skipping SMS sending.");
                return;
            }

            if (string.IsNullOrEmpty(templateId))
            {
                _logger.LogWarning("MSG91 SMS default TemplateId is not configured. Skipping SMS sending.");
                return;
            }

            // Ensure toPhone is clean and in international format (e.g. removing +)
            string cleanPhone = CleanPhoneNumber(toPhone);

            var payload = new
            {
                flow_id = templateId,
                recipients = new[]
                {
                    new Dictionary<string, string>
                    {
                        { "mobiles", cleanPhone },
                        { variableName, message }
                    }
                }
            };

            await SendMsg91SmsRequestAsync(payload, authKey);
        }

        private string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return string.Empty;
            // Remove '+' and spaces
            return phone.Replace("+", "").Replace(" ", "").Trim();
        }

        private async Task SendMsg91SmsRequestAsync(object payload, string authKey)
        {
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
                    _logger.LogError("MSG91 SMS Flow API call failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"MSG91 SMS Flow API call failed: {response.StatusCode} - {errorContent}");
                }
                _logger.LogInformation("SMS successfully sent via MSG91 Flow API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS via MSG91 Flow API.");
                throw;
            }
        }

        public async Task SendTemplatedSmsAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            var enabledStr = _configuration["SmsSettings:Enabled"] ?? "true";
            if (!bool.TryParse(enabledStr, out bool enabled) || !enabled)
            {
                _logger.LogInformation("SMS service is disabled in configuration. Skipping templated SMS sending.");
                return;
            }

            var provider = _configuration["SmsSettings:Provider"] ?? "Mock";

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendTemplatedSmsViaTwilioAsync(toPhone, templateId, variables);
            }
            else if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
            {
                await SendTemplatedSmsViaMsg91Async(toPhone, templateId, variables);
            }
            else
            {
                // Mock / Disabled
                // Render templates locally for logging
                string rawTemplate = GetDefaultLocalSmsTemplate(templateId);
                string message = ParseTemplate(rawTemplate, variables);

                _logger.LogWarning("------ MOCK TEMPLATED SMS SENT (SMS IS CURRENTLY DISABLED) ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Template ID: {TemplateId}", templateId);
                _logger.LogWarning("Rendered Message: {Message}", message);
                _logger.LogWarning("-----------------------------------------------------------------");
                await Task.CompletedTask;
            }
        }

        private async Task SendTemplatedSmsViaTwilioAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            // 1. Try to load template from database
            var dbTemplate = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.TemplateType == "SMS" && t.IsActive == 1);
            
            string? templateContent = dbTemplate?.HtmlContent;

            // 2. Fall back to configuration
            if (string.IsNullOrEmpty(templateContent))
            {
                templateContent = _configuration[$"SmsSettings:Templates:{templateId}"];
            }

            // 3. Fall back to default templates
            if (string.IsNullOrEmpty(templateContent))
            {
                templateContent = GetDefaultLocalSmsTemplate(templateId);
            }

            // Populate default variables like user name and clinic name if they aren't provided
            await PopulateDefaultVariablesAsync(toPhone, variables);

            // Parse variables
            string message = ParseTemplate(templateContent, variables);

            await SendSmsViaTwilioAsync(toPhone, message);
        }

        private async Task SendTemplatedSmsViaMsg91Async(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            var authKey = await GetSecretKeyAsync("Msg91Settings:AuthKey", "Msg91Settings:AuthKey");
            if (string.IsNullOrEmpty(authKey) || authKey.Contains("_PLACEHOLDER"))
            {
                _logger.LogWarning("MSG91 AuthKey is not configured. Skipping SMS sending.");
                return;
            }

            // Resolve MSG91 Flow ID from config mapping
            // e.g. Msg91Settings:Sms:Templates:ForgotPassword
            var flowId = _configuration[$"Msg91Settings:Sms:Templates:{templateId}"];
            if (string.IsNullOrEmpty(flowId))
            {
                // If not found in dictionary, try to use the templateId parameter directly as Flow ID
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

            await SendMsg91SmsRequestAsync(payload, authKey);
        }

        private async Task PopulateDefaultVariablesAsync(string toPhone, Dictionary<string, string> variables)
        {
            if (!variables.ContainsKey("user_name") && !variables.ContainsKey("UserName") && !variables.ContainsKey("user") && !variables.ContainsKey("PatientName"))
            {
                var user = await _context.Users
                    .Include(u => u.Contact)
                    .FirstOrDefaultAsync(u => u.Contact != null && (u.Contact.PrimaryPhone == toPhone || u.Contact.SecondaryPhone == toPhone) && u.IsActive == 1);
                
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

        private string GetDefaultLocalSmsTemplate(string templateId)
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
