using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface IWhatsAppService
    {
        Task SendWhatsAppAsync(string toPhone, string message);
        Task SendTemplatedWhatsAppAsync(string toPhone, string templateId, Dictionary<string, string> variables);
    }
}
