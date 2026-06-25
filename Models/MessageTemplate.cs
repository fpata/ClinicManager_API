using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("emailtemplate")]
    public class MessageTemplate : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string TemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TemplateType { get; set; } = "Email"; // "Email", "SMS", "WhatsApp"

        [MaxLength(200)]
        public string? Subject { get; set; } // Nullable, only used for Email

        [Required]
        [Column(TypeName = "longtext")]
        public string HtmlContent { get; set; } = string.Empty; // Holds template content/body
    }
}
