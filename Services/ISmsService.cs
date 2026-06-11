using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhone, string message);
    }
}
