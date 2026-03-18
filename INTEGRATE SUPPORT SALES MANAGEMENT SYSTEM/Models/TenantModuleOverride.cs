using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class TenantModuleOverride
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string ModuleKey { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        [StringLength(200)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
