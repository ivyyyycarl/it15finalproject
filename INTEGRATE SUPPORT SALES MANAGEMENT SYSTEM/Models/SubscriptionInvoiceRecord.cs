using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class SubscriptionInvoiceRecord
    {
        public int Id { get; set; }

        public int TenantSubscriptionId { get; set; }

        [StringLength(120)]
        public string? StripeInvoiceId { get; set; }

        [StringLength(120)]
        public string? StripePaymentIntentId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "open";

        public DateTime? DueDate { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TenantSubscription? TenantSubscription { get; set; }
    }
}
