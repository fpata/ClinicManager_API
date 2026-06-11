using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("patientreport")]
    public class PatientReport:BaseEntity   
    {
        public int? UserID { get; set; }
        public int? PatientID { get; set; }
        public string? ReportName { get; set; }
        public string? ReportDetails { get; set; }
        public string? ReportFilePath { get; set; }

        public int? DoctorID { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? ReportDate { get; set; } = DateTime.Now;    

    }
}
