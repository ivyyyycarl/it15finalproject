namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class ModuleEntitlementResultDto
    {
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool IsModuleEnabled { get; set; }
        public bool IsRoleAllowed { get; set; }
        public bool IsPlanIncluded { get; set; }
        public bool IsQuotaExceeded { get; set; }
        public string ReasonCode { get; set; } = "allowed";
        public string Message { get; set; } = "Allowed";
    }

    public class SubscriptionUsageMetricDto
    {
        public string Dimension { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Unit { get; set; } = "count";
        public decimal UsedQuantity { get; set; }
        public decimal AllowedQuantity { get; set; }
        public decimal UtilizationPercent { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsExceeded { get; set; }
    }

    public class SubscriptionUsageOverviewDto
    {
        public string TenantName { get; set; } = "ClassicFit";
        public string PlanName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<SubscriptionUsageMetricDto> Metrics { get; set; } = new();
    }
}
