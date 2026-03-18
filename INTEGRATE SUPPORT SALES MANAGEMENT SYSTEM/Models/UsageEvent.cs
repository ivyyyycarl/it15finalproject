using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class UsageEvent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Dimension { get; set; } = string.Empty; // tickets, calls, storage_mb

        public decimal Quantity { get; set; } = 1m;

        [StringLength(50)]
        public string Unit { get; set; } = "count";

        [StringLength(120)]
        public string? SourceType { get; set; }

        public int? SourceId { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
