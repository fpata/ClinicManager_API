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

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            // SMS sending is commented out/disabled for now. Running mock logging.
            _logger.LogWarning("------ MOCK SMS SENT (SMS IS CURRENTLY DISABLED) ------");
            _logger.LogWarning("To: {ToPhone}", toPhone);
            _logger.LogWarning("Message: {Message}", message);
            _logger.LogWarning("-----------------------------------------------------");
            await Task.CompletedTask;
        }

        public async Task SendTemplatedSmsAsync(string toPhone, string templateId, Dictionary<string, string> variables)
        {
            // SMS sending is commented out/disabled for now. Running mock logging.
            _logger.LogWarning("------ MOCK TEMPLATED SMS SENT (SMS IS CURRENTLY DISABLED) ------");
            _logger.LogWarning("To: {ToPhone}", toPhone);
            _logger.LogWarning("Template ID: {TemplateId}", templateId);
            _logger.LogWarning("Variables:");
            foreach (var kvp in variables)
            {
                _logger.LogWarning("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
            _logger.LogWarning("-----------------------------------------------------------------");
            await Task.CompletedTask;
        }
    }
}
