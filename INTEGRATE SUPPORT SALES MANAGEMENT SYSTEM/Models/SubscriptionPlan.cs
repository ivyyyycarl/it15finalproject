using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AnnualPrice { get; set; }

        public int MaxUsers { get; set; } = 10;

        public int MaxBranches { get; set; } = 1;

        public int MaxTicketsPerMonth { get; set; } = 1000;

        public int MaxCallLogsPerMonth { get; set; } = 1000;

        public int MaxStorageMb { get; set; } = 2048;

        public bool IsSoftLimit { get; set; } = true;

        [Column(TypeName = "decimal(5,2)")]
        public decimal SoftLimitGracePercent { get; set; } = 10m;

        [StringLength(2000)]
        public string IncludedModulesCsv { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
