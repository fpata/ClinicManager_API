using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManager.Models.Enums;

namespace ClinicManager.Models
{
    [Table("User")]
    public class User : BaseEntity
    {
      
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? MiddleName { get; set; }
        
      
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
      
        [StringLength(50)]
        public string? UserName { get; set; } = string.Empty;
        
      
        [StringLength(255)]
        public string? Password { get; set; } = string.Empty;
       
 
        public UserType? UserType { get; set; } = Enums.UserType.Patient;

        public int? Age { get; set; }

        public Gender? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }
        
        //[NotMapped]
        //public int? Age => DateOfBirth.HasValue 
        //    ? DateTime.Today.Year - DateOfBirth.Value.Year - 
        //      (DateTime.Today.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0) 
        //    : null;
        
        public DateTime? LastLoginDate { get; set; }
        
        [StringLength(100)]
        public string? Designation { get; set; }
        
        [StringLength(100)]
        public string? LicenseNumber { get; set; }
        
        [StringLength(200)]
        public string? Specialization { get; set; }
        
        public DateTime? LicenseExpiryDate { get; set; }
        
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
        
        // Navigation properties
        public virtual Address? Address { get; set; }
        public virtual Contact? Contact { get; set; }
        public virtual ICollection<Patient>? Patients { get; set; }
    }
}
