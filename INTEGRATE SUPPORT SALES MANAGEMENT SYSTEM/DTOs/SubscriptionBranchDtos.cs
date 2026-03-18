using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class SubscriptionPlanDto
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
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999999999)]
        public decimal MonthlyPrice { get; set; }

        [Range(0, 999999999)]
        public decimal AnnualPrice { get; set; }

        [Range(1, 100000)]
        public int MaxUsers { get; set; } = 10;

        [Range(1, 100000)]
        public int MaxBranches { get; set; } = 1;

        [Range(1, 10000000)]
        public int MaxTicketsPerMonth { get; set; } = 1000;

        [Range(1, 10000000)]
        public int MaxCallLogsPerMonth { get; set; } = 1000;

        [Range(1, 10000000)]
        public int MaxStorageMb { get; set; } = 2048;

        public bool IsSoftLimit { get; set; } = true;

        [Range(0, 100)]
        public decimal SoftLimitGracePercent { get; set; } = 10m;

        [StringLength(2000)]
        public string IncludedModulesCsv { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class TenantSubscriptionDto
    {
        public int Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? NextBillingAt { get; set; }
        public bool AutoRenew { get; set; }
        public int SubscriptionPlanId { get; set; }
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
        [Range(1, int.MaxValue)]
        public int SubscriptionPlanId { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public bool AutoRenew { get; set; } = true;
        public string BillingCycle { get; set; } = "Monthly";

        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? NextBillingAt { get; set; }
    }

    public class BranchDto
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
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        public string? AddressLine { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? ZipCode { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AssignUserBranchRequest
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        public int? BranchId { get; set; }
    }

    public enum BillingCycle
    {
        Monthly = 1,
        Annual = 2
    }

    public class CompanySubscriptionRequest
    {
        [Required]
        [StringLength(120)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AdminFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AdminLastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters with uppercase, lowercase, number, and special character")]
        public string AdminPassword { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [Range(1, int.MaxValue)]
        public int SubscriptionPlanId { get; set; }

        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

        [StringLength(150)]
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
}
