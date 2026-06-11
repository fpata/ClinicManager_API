using ClinicManager.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClinicManager.Models
{
    [Table("patienttreatmentdetail")]
    public class PatientTreatmentDetail: BaseEntity
    {
    
        [ForeignKey("PatientTreatment")]
        public int? PatientTreatmentID { get; set; }
        
        public int? UserID { get; set; }
        public string? Tooth { get; set; }
        public string? Procedure { get; set; }
        public string? Prescription { get; set; }
        public DateTime? TreatmentDate { get; set; }
        
        [ForeignKey("Patient")]
        public int? PatientID { get; set; }

        public string? FollowUpInstructions { get; set; }

        public string? FollowUpDate { get; set; }

        public float? ProcedureTreatmentCost { get; set; } = 0;

        // Navigation properties to enable EF Core relationship tracking
        public virtual PatientTreatment? PatientTreatment { get; set; }
        public virtual Patient? Patient { get; set; }

    }
}
