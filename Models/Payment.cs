using ClinicManager.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    [Table("Payment")]
    public class Payment : BaseEntity
    {
        public int? BillingID { get; set; }
        public float? Amount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? TransactionDate { get; set; }   // ISO datetime
        public string? Reference { get; set; }        // Check #, Auth code, etc.
        public string? Notes { get; set; }
    }
}
