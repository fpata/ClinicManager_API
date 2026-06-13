using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhone, string message);
        Task SendTemplatedSmsAsync(string toPhone, string templateId, Dictionary<string, string> variables);
    }
}
