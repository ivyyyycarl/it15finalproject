using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class TenantSubscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string TenantName { get; set; } = "ClassicFit";

        public int SubscriptionPlanId { get; set; }

        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public DateTime StartsAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndsAt { get; set; }

        public DateTime? NextBillingAt { get; set; }

        [StringLength(20)]
        public string BillingCycle { get; set; } = "Monthly";

        public DateTime? CurrentPeriodStart { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        [StringLength(120)]
        public string? StripeCustomerId { get; set; }

        [StringLength(120)]
        public string? StripeSubscriptionId { get; set; }

        [StringLength(25)]
        public string Currency { get; set; } = "PHP";

        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        [StringLength(60)]
        public string? LastPaymentStatus { get; set; }

        public DateTime? LastPaymentAt { get; set; }

        public DateTime? TrialStartsAt { get; set; }

        public DateTime? TrialEndsAt { get; set; }

        public DateTime? CanceledAt { get; set; }

        [StringLength(255)]
        public string? CancelReason { get; set; }

        public bool AutoRenew { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum SubscriptionStatus
    {
        Active = 1,
        Trial = 2,
        PastDue = 3,
        Canceled = 4,
        Expired = 5
    }
}
