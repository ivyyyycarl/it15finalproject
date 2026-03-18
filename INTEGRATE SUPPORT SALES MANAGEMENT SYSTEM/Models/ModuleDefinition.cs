using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class ModuleDefinition
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string ModuleKey { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(80)]
        public string Category { get; set; } = "General";

        public bool IsActive { get; set; } = true;

        public bool AllowAdmin { get; set; } = true;

        public bool AllowSupervisor { get; set; } = true;

        public bool AllowAgent { get; set; } = true;

        public bool AllowCustomer { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PlanModuleEntitlement> PlanEntitlements { get; set; } = new List<PlanModuleEntitlement>();
    }
}
