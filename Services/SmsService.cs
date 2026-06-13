using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string DecodeBase64Key(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return input;
            }
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            var authKey = DecodeBase64Key(_configuration["Msg91Settings:AuthKey"]);
            var templateId = _configuration["Msg91Settings:Sms:TemplateId"];
            var varName = _configuration["Msg91Settings:Sms:VariableName"] ?? "message";

            if (string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(templateId))
            {
                _logger.LogWarning("------ MOCK SMS SENT ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Message: {Message}", message);
                _logger.LogWarning("----------------------------");
                return;
            }

            try
            {
                var requestUrl = "https://control.msg91.com/api/v5/flow/";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                request.Headers.Add("authkey", authKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Strip '+' from phone number if present
                var cleanPhone = toPhone.Trim().Replace("+", "");

                // Build recipient object with dynamic variable name
                var recipient = new Dictionary<string, string>
                {
                    { "mobiles", cleanPhone },
                    { varName, message }
                };

                var payload = new
                {
                    template_id = templateId,
                    short_url = "1",
                    recipients = new[] { recipient }
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS successfully sent to {ToPhone} via MSG91", toPhone);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send SMS via MSG91. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                    throw new Exception($"MSG91 SMS API returned status code {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {ToPhone} via MSG91.", toPhone);
                throw;
            }
        }

        public async Task SendTemplatedSmsAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            var authKey = DecodeBase64Key(_configuration["Msg91Settings:AuthKey"]);
            
            var configuredTemplateId = _configuration[$"Msg91Settings:Sms:Templates:{templateId}"];
            var actualTemplateId = !string.IsNullOrEmpty(configuredTemplateId) ? configuredTemplateId : templateId;

            if (string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(actualTemplateId))
            {
                _logger.LogWarning("------ MOCK TEMPLATED SMS SENT ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Template ID: {TemplateId}", actualTemplateId);
                _logger.LogWarning("Variables:");
                foreach (var kvp in variables)
                {
                    _logger.LogWarning("  {Key}: {Value}", kvp.Key, kvp.Value);
                }
                _logger.LogWarning("-------------------------------------");
                return;
            }

            try
            {
                var requestUrl = "https://control.msg91.com/api/v5/flow/";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                request.Headers.Add("authkey", authKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var cleanPhone = toPhone.Trim().Replace("+", "");

                var recipient = new Dictionary<string, string>
                {
                    { "mobiles", cleanPhone }
                };
                foreach (var kvp in variables)
                {
                    recipient[kvp.Key] = kvp.Value;
                }

                var payload = new
                {
                    template_id = actualTemplateId,
                    short_url = "1",
                    recipients = new[] { recipient }
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Templated SMS ({TemplateId}) successfully sent to {ToPhone} via MSG91", actualTemplateId, toPhone);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send templated SMS ({TemplateId}) via MSG91. Status: {StatusCode}, Response: {Response}", actualTemplateId, response.StatusCode, responseContent);
                    throw new Exception($"MSG91 SMS API returned status code {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated SMS ({TemplateId}) to {ToPhone} via MSG91.", actualTemplateId, toPhone);
                throw;
            }
        }
    }
}
