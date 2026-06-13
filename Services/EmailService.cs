using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
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

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var authKey = DecodeBase64Key(_configuration["Msg91Settings:AuthKey"]);
            var domain = _configuration["Msg91Settings:Email:Domain"];
            var templateId = _configuration["Msg91Settings:Email:TemplateId"];
            var senderEmail = _configuration["Msg91Settings:Email:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["Msg91Settings:Email:SenderName"] ?? "Relief Dental Clinic";
            var clinic_name = _configuration["Msg91Settings:Email:ClinicName"];
            if (string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(templateId) || string.IsNullOrEmpty(domain))
            {
                _logger.LogWarning("------ MOCK EMAIL SENT ------");
                _logger.LogWarning("To: {ToEmail}", toEmail);
                _logger.LogWarning("Subject: {Subject}", subject);
                _logger.LogWarning("Body:\n{Body}", body);
                _logger.LogWarning("-----------------------------");
                return;
            }

            try
            {
                var requestUrl = "https://control.msg91.com/api/v5/email/send";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                request.Headers.Add("authkey", authKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    recipients = new[]
                    {
                        new
                        {
                            to = new[]
                            {
                                new { name = toEmail, email = toEmail  }
                            },
                            variables = new
                            {
                                subject = subject,
                                body = body,
                                clinic_name = clinic_name
                            }
                        }
                    },
                    from = new
                    {
                        name = senderName,
                        email = senderEmail
                    },
                    domain = domain,
                    template_id = templateId
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email successfully sent to {ToEmail} via MSG91", toEmail);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send email via MSG91. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                    throw new Exception($"MSG91 Email API returned status code {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} via MSG91.", toEmail);
                throw;
            }
        }

        public async Task SendTemplatedEmailAsync(string toEmail, string templateId, Dictionary<string, string> variables)
        {
            var authKey = DecodeBase64Key(_configuration["Msg91Settings:AuthKey"]);
            var domain = _configuration["Msg91Settings:Email:Domain"];
            var senderEmail = _configuration["Msg91Settings:Email:SenderEmail"] ?? "noreply@reliefdentalclinic.com";
            var senderName = _configuration["Msg91Settings:Email:SenderName"] ?? "Relief Dental Clinic";
            var clinic_name = _configuration["Msg91Settings:Email:ClinicName"];
            
            string activeClinicName = !string.IsNullOrEmpty(clinic_name) ? clinic_name : "Relief Dental Clinic";
            variables.Add("clinic_name", activeClinicName);

            var configuredTemplateId = _configuration[$"Msg91Settings:Email:Templates:{templateId}"];
            var actualTemplateId = !string.IsNullOrEmpty(configuredTemplateId) ? configuredTemplateId : templateId;

            if (string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(actualTemplateId))
            {
                _logger.LogWarning("------ MOCK TEMPLATED EMAIL SENT ------");
                _logger.LogWarning("To: {ToEmail}", toEmail);
                _logger.LogWarning("Template ID: {TemplateId}", actualTemplateId);
                _logger.LogWarning("Variables:");
                foreach (var kvp in variables)
                {
                    _logger.LogWarning("  {Key}: {Value}", kvp.Key, kvp.Value);
                }
                _logger.LogWarning("---------------------------------------");
                return;
            }

            try
            {
                var requestUrl = "https://control.msg91.com/api/v5/email/send";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                request.Headers.Add("authkey", authKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    recipients = new[]
                    {
                        new
                        {
                            to = new[]
                            {
                                new { name = toEmail, email = toEmail }
                            },
                            variables = variables
                        }
                    },
                    from = new
                    {
                        name = senderName,
                        email = senderEmail
                    },
                    domain = domain,
                    template_id = actualTemplateId
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Templated email ({TemplateId}) successfully sent to {ToEmail} via MSG91", actualTemplateId, toEmail);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send templated email ({TemplateId}) via MSG91. Status: {StatusCode}, Response: {Response}", actualTemplateId, response.StatusCode, responseContent);
                    throw new Exception($"MSG91 Email API returned status code {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated email ({TemplateId}) to {ToEmail} via MSG91.", actualTemplateId, toEmail);
                throw;
            }
        }
    }
}
