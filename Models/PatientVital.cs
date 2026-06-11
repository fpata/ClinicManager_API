using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManager.Models;

namespace ClinicManager.Models
{
    [Table("patientvitals")]
    public class PatientVitals : BaseEntity
    {
        public int? UserID { get; set; }
      
        public int? PatientID { get; set; }
        
      
        public DateTime? RecordedDate { get; set; } = DateTime.Now;
        
        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }
        public int? HeartRate { get; set; }
        public string? Temperature { get; set; } // in Celsius
        public string? Weight { get; set; } // in kg
        public string? Height { get; set; } // in cm
        public int? OxygenSaturation { get; set; }
        public int? RespiratoryRate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }


        [StringLength(100)]
        public string? BloodType { get; set; }

        public int? SugarFasting { get; set; }
        public int? SugarRandom { get; set; }

        public int? SugarPostPrandial { get; set; }


        public int? RecordedBy { get; set; } = 1;


        //[NotMapped]
        //public double? BMI => Weight.HasValue && Height.HasValue && Height > 0
        //    ? Math.Round((double)(Weight / Math.Pow((double)(Height / 100), 2)), 2)
        //    : null;


        //[NotMapped]
        //public string BloodPressure => BloodPressureSystolic.HasValue && BloodPressureDiastolic.HasValue 
        //    ? $"{BloodPressureSystolic}/{BloodPressureDiastolic}" 
        //    : string.Empty;
   
    }
}