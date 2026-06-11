using System.Threading.Tasks;

namespace ClinicManager.Services
{
    public interface IPrescriptionService
    {
        Task<byte[]> GeneratePrescriptionRtfAsync(int treatmentId, int? treatmentDetailId, bool includeHeader, string? doctorNotes);
    }
}
