using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("roleaccess")]
    public class RoleAccess : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public bool CanAccessPatient { get; set; } = false;

        public bool CanAccessDashboard { get; set; } = false;

        public bool CanAccessBilling { get; set; } = false;

        public bool CanAccessConfig { get; set; } = false;
    }
}
