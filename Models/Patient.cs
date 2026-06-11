using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("patient")]
    public class Patient : BaseEntity
    {
      
        public int UserID { get; set; }
        
        [StringLength(1000)]
        public string? Allergies { get; set; }
        
        [StringLength(1000)]
        public string? Medications { get; set; }
        
        [StringLength(500)]
        public string? FatherMedicalHistory { get; set; }

        [StringLength(500)]
        public string? MotherMedicalHistory { get; set; }

        [StringLength(500)]
        public string? PersonalMedicalHistory { get; set; }
      
        [StringLength(50)]
        public string? InsuranceProvider { get; set; }
        
        [StringLength(100)]
        public string? InsurancePolicyNumber { get; set; }
        
        public virtual ICollection<PatientAppointment>? PatientAppointments { get; set; }
        public virtual ICollection<PatientReport>? PatientReports { get; set; }
        public virtual PatientTreatment? PatientTreatment { get; set; }
        
        public virtual ICollection<PatientVitals>? PatientVitals { get; set; }
    }
}
