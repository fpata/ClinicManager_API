using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
    
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        

        public DateTime? ModifiedDate { get; set; } = DateTime.Now;
        

        public int? CreatedBy { get; set; } = 1;
        

        public int? ModifiedBy { get; set; } = 1;

        public byte? IsActive { get; set; } = 1;

      
    }
}