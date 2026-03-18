namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class SuperAdminAnalyticsOverviewDto
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int PastDueSubscriptions { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AnnualRunRate { get; set; }
        public decimal CollectedRevenueLast30Days { get; set; }
        public List<PlanAdoptionMetricDto> PlanAdoption { get; set; } = new();
        public List<ModuleUsageMetricDto> ModuleUsage { get; set; } = new();
    }

    public class PlanAdoptionMetricDto
    {
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public int TenantCount { get; set; }
    }

    public class ModuleUsageMetricDto
    {
        public string ModuleKey { get; set; } = string.Empty;
        public decimal UsedQuantity { get; set; }
    }
}
