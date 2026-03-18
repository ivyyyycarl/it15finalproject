using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class PendingSubscriptionOnboarding
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AdminFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AdminLastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public int SubscriptionPlanId { get; set; }

        [StringLength(20)]
        public string BillingCycle { get; set; } = "Monthly";

        [StringLength(150)]
        public string? InitialBranchName { get; set; }

        public int AdminUserId { get; set; }

        [StringLength(255)]
        public string? CheckoutSessionId { get; set; }

        [StringLength(120)]
        public string? StripeCustomerId { get; set; }

        [StringLength(50)]
        public string CheckoutStatus { get; set; } = "created";

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}
