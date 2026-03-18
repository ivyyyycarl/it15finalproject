namespace SupportSalesManagement.Frontend.Models
{
    public enum SubscriptionStatus
    {
        Active = 1,
        Trial = 2,
        PastDue = 3,
        Canceled = 4,
        Expired = 5
    }

    public class SubscriptionPlanModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public int MaxTicketsPerMonth { get; set; }
        public int MaxCallLogsPerMonth { get; set; }
        public int MaxStorageMb { get; set; }
        public bool IsSoftLimit { get; set; }
        public decimal SoftLimitGracePercent { get; set; }
        public string IncludedModulesCsv { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UpsertSubscriptionPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public int MaxUsers { get; set; } = 10;
        public int MaxBranches { get; set; } = 1;
        public int MaxTicketsPerMonth { get; set; } = 1000;
        public int MaxCallLogsPerMonth { get; set; } = 1000;
        public int MaxStorageMb { get; set; } = 2048;
        public bool IsSoftLimit { get; set; } = true;
        public decimal SoftLimitGracePercent { get; set; } = 10m;
        public string IncludedModulesCsv { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class TenantSubscriptionModel
    {
        public int Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? NextBillingAt { get; set; }
        public bool AutoRenew { get; set; }
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public string Currency { get; set; } = "PHP";
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? LastPaymentStatus { get; set; }
        public DateTime? LastPaymentAt { get; set; }
        public DateTime? TrialStartsAt { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public string? CancelReason { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public int MaxTicketsPerMonth { get; set; }
        public int MaxCallLogsPerMonth { get; set; }
        public int MaxStorageMb { get; set; }
        public string IncludedModulesCsv { get; set; } = string.Empty;
    }

    public class UpdateTenantSubscriptionRequest
    {
        public int SubscriptionPlanId { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public bool AutoRenew { get; set; } = true;
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? NextBillingAt { get; set; }
    }

    public class BranchModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public bool IsActive { get; set; }
        public int AssignedUsersCount { get; set; }
    }

    public class UpsertBranchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum BillingCycle
    {
        Monthly = 1,
        Annual = 2
    }

    public class CompanySubscriptionRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string AdminFirstName { get; set; } = string.Empty;
        public string AdminLastName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public int SubscriptionPlanId { get; set; }
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
        public string? InitialBranchName { get; set; }
    }

    public class CompanySubscriptionResponse
    {
        public int TenantSubscriptionId { get; set; }
        public int AdminUserId { get; set; }
        public int BranchId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ModuleEntitlementResultModel
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

    public class SubscriptionUsageMetricModel
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

    public class SubscriptionUsageOverviewModel
    {
        public string TenantName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<SubscriptionUsageMetricModel> Metrics { get; set; } = new();
    }

    public class CreatePlanChangeCheckoutRequest
    {
        public int SubscriptionPlanId { get; set; }
        public string BillingCycle { get; set; } = "Monthly";
    }

    public class CheckoutSessionResponseModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
    }

    public class PlanModuleEntitlementModel
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
        public List<PlanModuleEntitlementModel> Modules { get; set; } = new();
    }

    public class SuperAdminGovernanceAnalyticsModel
    {
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRecurringRevenueEstimate { get; set; }
        public decimal RevenueCollectedCurrentPeriod { get; set; }
        public List<PlanAdoptionItemModel> PlanAdoption { get; set; } = new();
        public List<ModuleUsageItemModel> ModuleUsage { get; set; } = new();
    }

    public class PlanAdoptionItemModel
    {
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public int TenantCount { get; set; }
    }

    public class ModuleUsageItemModel
    {
        public string Dimension { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
    }
}
