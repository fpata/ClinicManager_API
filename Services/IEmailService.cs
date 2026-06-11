using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
