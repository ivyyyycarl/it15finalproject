using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IEntitlementService
    {
        Task<ModuleEntitlementResultDto> EvaluateModuleAccessAsync(UserRole role, string moduleKey, string? tenantName = null);
        Task<List<ModuleEntitlementResultDto>> GetModuleAccessMapAsync(UserRole role, string? tenantName = null);
        Task<SubscriptionUsageOverviewDto?> GetSubscriptionUsageOverviewAsync(string? tenantName = null);
        Task RecordUsageAsync(string dimension, decimal quantity = 1m, string? unit = null, string? sourceType = null, int? sourceId = null, string? tenantName = null);
    }
}
