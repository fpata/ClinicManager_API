using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("systemkey")]
    public class SystemKey : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string KeyName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "longtext")]
        public string KeyValue { get; set; } = string.Empty;
    }
}
