namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class PlanModuleEntitlement
    {
        public int Id { get; set; }

        public int SubscriptionPlanId { get; set; }

        public int ModuleDefinitionId { get; set; }

        public bool IsIncluded { get; set; } = true;

        public bool AllowAdmin { get; set; } = true;

        public bool AllowSupervisor { get; set; } = true;

        public bool AllowAgent { get; set; } = true;

        public bool AllowCustomer { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public ModuleDefinition? ModuleDefinition { get; set; }
    }
}
