using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("patienttreatment")]
    public class PatientTreatment : BaseEntity
    {
      
        public int? UserID { get; set; }

      
        public int? PatientID { get; set; }
        
      
        public int? DoctorID { get; set; }
        
        public int? AppointmentID { get; set; }
        
      
        [StringLength(500)]
        public string ChiefComplaint { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? ClinicalFindings { get; set; }
        
        [StringLength(500)]
        public string? Diagnosis { get; set; }
        
        [StringLength(1000)]
        public string? TreatmentPlan { get; set; }
        
        [StringLength(500)]
        public string? Prescription { get; set; }
        

      
        
        [StringLength(50)]
        public string? PaymentStatus { get; set; }

        public float? EstimatedCost { get; set; }
        public float? ActualCost { get; set; }

        public virtual ICollection<PatientTreatmentDetail>? PatientTreatmentDetails { get; set; } = new List<PatientTreatmentDetail>();

        public BillingRecord? BillingRecord { get; set; }
    }
}
