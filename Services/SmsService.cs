using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly HttpClient _httpClient;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            var accountSid = _configuration["SmsSettings:AccountSid"];
            var authToken = _configuration["SmsSettings:AuthToken"];
            var fromPhoneNumber = _configuration["SmsSettings:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhoneNumber))
            {
                _logger.LogWarning("------ MOCK SMS SENT ------");
                _logger.LogWarning("To: {ToPhone}", toPhone);
                _logger.LogWarning("Message: {Message}", message);
                _logger.LogWarning("----------------------------");
                return;
            }

            try
            {
                var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                // Twilio uses basic auth
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var contentList = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("To", toPhone),
                    new KeyValuePair<string, string>("From", fromPhoneNumber),
                    new KeyValuePair<string, string>("Body", message)
                };

                request.Content = new FormUrlEncodedContent(contentList);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS successfully sent to {ToPhone}", toPhone);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send SMS via Twilio. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                    throw new Exception($"Twilio API returned status code {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {ToPhone} via Twilio.", toPhone);
                throw;
            }
        }
    }
}
