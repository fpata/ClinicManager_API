using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendTemplatedEmailAsync(string toEmail, string templateId, Dictionary<string, string> variables);
    }
}
