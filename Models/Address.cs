using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("Address")]
    public class Address : BaseEntity
    {
        [StringLength(200)]
      
        public string? PermAddress1 { get; set; }
        [StringLength(200)]
        public string? PermAddress2 { get; set; }
        [StringLength(45)]
        public string? PermState { get; set; }
        [StringLength(45)]
        public string? PermCity { get; set; }
        [StringLength(45)]
        public string? PermCountry { get; set; }
        [StringLength(45)]
        public string? PermZipCode { get; set; }
        [StringLength(200)]
        public string? CorrAddress1 { get; set; }
        [StringLength(200)]
        public string? CorrAddress2 { get; set; }
        [StringLength(45)]
        public string? CorrState { get; set; }
        [StringLength(45)]
        public string? CorrCity { get; set; }
        [StringLength(45)]
        public string? CorrCountry { get; set; }
        [StringLength(45)]
        public string? CorrZipCode { get; set; }

        [ForeignKey("User")]
        public int? UserID { get; set; }
    }
}
