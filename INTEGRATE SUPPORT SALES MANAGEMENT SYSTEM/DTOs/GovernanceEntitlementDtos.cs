namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class PlanModuleEntitlementDto
    {
        public int ModuleDefinitionId { get; set; }
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsIncluded { get; set; }
        public bool AllowAdmin { get; set; }
        public bool AllowSupervisor { get; set; }
        public bool AllowAgent { get; set; }
        public bool AllowCustomer { get; set; }
    }

    public class UpdatePlanModuleEntitlementsRequest
    {
        public List<PlanModuleEntitlementDto> Modules { get; set; } = new();
    }

    public class CreatePlanChangeCheckoutRequest
    {
        public int SubscriptionPlanId { get; set; }
        public string BillingCycle { get; set; } = "Monthly";
    }

    public class CheckoutSessionResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
    }

    public class SuperAdminGovernanceAnalyticsDto
    {
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRecurringRevenueEstimate { get; set; }
        public decimal RevenueCollectedCurrentPeriod { get; set; }
        public List<PlanAdoptionItemDto> PlanAdoption { get; set; } = new();
        public List<ModuleUsageItemDto> ModuleUsage { get; set; } = new();
    }

    public class PlanAdoptionItemDto
    {
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public int TenantCount { get; set; }
    }

    public class ModuleUsageItemDto
    {
        public string Dimension { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
    }
}
