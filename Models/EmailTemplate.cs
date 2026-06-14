using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("emailtemplate")]
    public class EmailTemplate : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string TemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "longtext")]
        public string HtmlContent { get; set; } = string.Empty;
    }
}
