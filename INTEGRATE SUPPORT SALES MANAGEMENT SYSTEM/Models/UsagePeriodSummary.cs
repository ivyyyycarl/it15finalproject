using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class UsagePeriodSummary
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Dimension { get; set; } = string.Empty;

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public decimal UsedQuantity { get; set; }

        public decimal AllowedQuantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
