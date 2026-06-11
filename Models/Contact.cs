using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ClinicManager.Models
{
    [Table("contact")]
    public class Contact:BaseEntity
    {
      
        [StringLength(100)]
        public string PrimaryPhone { get; set; } = string.Empty;
        [StringLength(100)]
        public string? SecondaryPhone { get; set; }
        [StringLength(100)]
        public string PrimaryEmail { get; set; } = string.Empty;
        [StringLength(100)]
        public string? SecondaryEmail { get; set; }
        [StringLength(200)]
        public string? RelativeName { get; set; }
        [StringLength(200)]
        public string? RelativeRealtion { get; set; }
        [StringLength(200)]
        public string? RelativePhone { get; set; }
        [StringLength(100)]
        public string? RelativeEmail { get; set; }

        [ForeignKey("User")]
        public int? UserID { get; set; } = 1;
    }
}
