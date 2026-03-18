using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class EntitlementService : IEntitlementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IModuleManagementService _moduleManagementService;

        public EntitlementService(ApplicationDbContext context, IModuleManagementService moduleManagementService)
        {
            _context = context;
            _moduleManagementService = moduleManagementService;
        }

        public async Task<ModuleEntitlementResultDto> EvaluateModuleAccessAsync(UserRole role, string moduleKey, string? tenantName = null)
        {
            var normalizedModuleKey = (moduleKey ?? string.Empty).Trim().ToLowerInvariant();
            var result = new ModuleEntitlementResultDto
            {
                ModuleKey = normalizedModuleKey,
                DisplayName = normalizedModuleKey,
                IsVisible = true,
                IsModuleEnabled = true,
                IsRoleAllowed = true,
                IsPlanIncluded = true,
                IsQuotaExceeded = false,
                ReasonCode = "allowed",
                Message = "Allowed"
            };

            if (string.IsNullOrWhiteSpace(normalizedModuleKey))
            {
                result.IsVisible = false;
                result.ReasonCode = "role_blocked";
                result.Message = "Invalid module key.";
                return result;
            }

            if (role == UserRole.SuperAdmin)
            {
                return result;
            }

            var moduleConfig = await _moduleManagementService.GetConfigurationAsync();
            var modulePolicy = moduleConfig.Modules.FirstOrDefault(m =>
                string.Equals(m.ModuleKey, normalizedModuleKey, StringComparison.OrdinalIgnoreCase));

            if (modulePolicy != null)
            {
                result.DisplayName = modulePolicy.DisplayName;
                result.IsModuleEnabled = modulePolicy.IsEnabled;
                result.IsRoleAllowed = role switch
                {
                    UserRole.Admin => modulePolicy.RoleAccess.Admin,
                    UserRole.Supervisor => modulePolicy.RoleAccess.Supervisor,
                    UserRole.Agent => modulePolicy.RoleAccess.Agent,
                    UserRole.Customer => modulePolicy.RoleAccess.Customer,
                    _ => false
                };
            }

            var currentSubscription = await GetCurrentSubscriptionAsync(tenantName);
            if (currentSubscription != null)
            {
                var moduleDefinition = await _context.ModuleDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleKey == normalizedModuleKey);

                var planEntitlement = moduleDefinition == null
                    ? null
                    : await _context.PlanModuleEntitlements
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e =>
                            e.SubscriptionPlanId == currentSubscription.SubscriptionPlanId &&
                            e.ModuleDefinitionId == moduleDefinition.Id);

                if (planEntitlement != null)
                {
                    result.IsPlanIncluded = planEntitlement.IsIncluded && role switch
                    {
                        UserRole.Admin => planEntitlement.AllowAdmin,
                        UserRole.Supervisor => planEntitlement.AllowSupervisor,
                        UserRole.Agent => planEntitlement.AllowAgent,
                        UserRole.Customer => planEntitlement.AllowCustomer,
                        _ => false
                    };
                }
                else
                {
                    result.IsPlanIncluded = IsIncludedByCsv(currentSubscription.SubscriptionPlan?.IncludedModulesCsv, normalizedModuleKey);
                }

                var tenantModuleOverride = await _context.TenantModuleOverrides
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o =>
                        o.TenantName == currentSubscription.TenantName &&
                        o.ModuleKey == normalizedModuleKey);
                if (tenantModuleOverride != null)
                {
                    result.IsPlanIncluded = tenantModuleOverride.IsEnabled;
                    if (!tenantModuleOverride.IsEnabled)
                    {
                        result.ReasonCode = "plan_excluded";
                        result.Message = $"Module disabled by tenant override. {tenantModuleOverride.Reason}".Trim();
                    }
                }

                var usage = await GetSubscriptionUsageOverviewAsync(currentSubscription.TenantName);
                if (usage != null)
                {
                    var quotaDimension = ResolveQuotaDimension(normalizedModuleKey);
                    if (!string.IsNullOrWhiteSpace(quotaDimension))
                    {
                        var metric = usage.Metrics.FirstOrDefault(m =>
                            string.Equals(m.Dimension, quotaDimension, StringComparison.OrdinalIgnoreCase));
                        if (metric != null && metric.IsExceeded)
                        {
                            result.IsQuotaExceeded = true;
                            result.ReasonCode = "quota_exceeded";
                            result.Message = $"Quota exceeded for {metric.Label}. Upgrade subscription to continue.";
                        }
                    }
                }
            }

            if (!result.IsModuleEnabled || !result.IsRoleAllowed)
            {
                result.IsVisible = false;
                result.ReasonCode = "role_blocked";
                result.Message = "Blocked by module role policy.";
                return result;
            }

            if (!result.IsPlanIncluded)
            {
                result.IsVisible = false;
                if (result.ReasonCode == "allowed")
                {
                    result.ReasonCode = "plan_excluded";
                    result.Message = "Module not included in current subscription plan.";
                }
                return result;
            }

            if (result.IsQuotaExceeded)
            {
                result.IsVisible = false;
                return result;
            }

            result.IsVisible = true;
            result.ReasonCode = "allowed";
            result.Message = "Allowed";
            return result;
        }

        public async Task<List<ModuleEntitlementResultDto>> GetModuleAccessMapAsync(UserRole role, string? tenantName = null)
        {
            var moduleConfig = await _moduleManagementService.GetConfigurationAsync();
            var result = new List<ModuleEntitlementResultDto>();
            foreach (var module in moduleConfig.Modules.OrderBy(m => m.Category).ThenBy(m => m.DisplayName))
            {
                result.Add(await EvaluateModuleAccessAsync(role, module.ModuleKey, tenantName));
            }

            return result;
        }

        public async Task<SubscriptionUsageOverviewDto?> GetSubscriptionUsageOverviewAsync(string? tenantName = null)
        {
            var currentSubscription = await GetCurrentSubscriptionAsync(tenantName);
            if (currentSubscription?.SubscriptionPlan == null)
            {
                return null;
            }

            var periodStart = currentSubscription.CurrentPeriodStart ?? currentSubscription.StartsAt;
            var periodEnd = currentSubscription.CurrentPeriodEnd ?? ResolvePeriodEnd(periodStart, currentSubscription.BillingCycle);

            var usageEvents = await _context.UsageEvents
                .AsNoTracking()
                .Where(e => e.TenantName == currentSubscription.TenantName &&
                            e.OccurredAt >= periodStart &&
                            e.OccurredAt < periodEnd)
                .ToListAsync();

            var metrics = new List<SubscriptionUsageMetricDto>
            {
                BuildMetric("tickets", "Tickets", "count", usageEvents, currentSubscription.SubscriptionPlan.MaxTicketsPerMonth, currentSubscription.SubscriptionPlan),
                BuildMetric("calls", "Call Logs", "count", usageEvents, currentSubscription.SubscriptionPlan.MaxCallLogsPerMonth, currentSubscription.SubscriptionPlan),
                BuildMetric("storage_mb", "Storage", "mb", usageEvents, currentSubscription.SubscriptionPlan.MaxStorageMb, currentSubscription.SubscriptionPlan)
            };

            await UpsertUsagePeriodSummariesAsync(currentSubscription.TenantName, periodStart, periodEnd, metrics);

            return new SubscriptionUsageOverviewDto
            {
                TenantName = currentSubscription.TenantName,
                PlanName = currentSubscription.SubscriptionPlan.Name,
                BillingCycle = currentSubscription.BillingCycle,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Metrics = metrics
            };
        }

        public async Task RecordUsageAsync(string dimension, decimal quantity = 1m, string? unit = null, string? sourceType = null, int? sourceId = null, string? tenantName = null)
        {
            if (quantity <= 0)
            {
                return;
            }

            var currentSubscription = await GetCurrentSubscriptionAsync(tenantName);
            if (currentSubscription == null)
            {
                return;
            }

            var normalizedDimension = (dimension ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedDimension))
            {
                return;
            }

            var usageEvent = new UsageEvent
            {
                TenantName = currentSubscription.TenantName,
                Dimension = normalizedDimension,
                Quantity = quantity,
                Unit = string.IsNullOrWhiteSpace(unit) ? "count" : unit.Trim().ToLowerInvariant(),
                SourceType = sourceType,
                SourceId = sourceId,
                OccurredAt = DateTime.UtcNow
            };
            _context.UsageEvents.Add(usageEvent);
            await _context.SaveChangesAsync();
        }

        private async Task<TenantSubscription?> GetCurrentSubscriptionAsync(string? tenantName)
        {
            var query = _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tenantName))
            {
                query = query.Where(s => s.TenantName == tenantName.Trim());
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private static bool IsIncludedByCsv(string? includedModulesCsv, string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(includedModulesCsv))
            {
                return true;
            }

            var rawTokens = includedModulesCsv
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            if (rawTokens.Count == 0 || rawTokens.Any(t => t.Equals("All", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var normalized = rawTokens
                .Select(NormalizeIncludedModuleToken)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return normalized.Contains(moduleKey);
        }

        private static string NormalizeIncludedModuleToken(string token)
        {
            return token.Trim().ToLowerInvariant() switch
            {
                "dashboard" => "dashboard",
                "users" or "iam" or "modulemanagement" => "iam",
                "catalog" or "inventory" or "products" => "catalog",
                "orders" or "returns" => "orders",
                "tickets" or "support" => "tickets",
                "calls" => "calls",
                "customers" or "crm" => "customers",
                "reports" or "analytics" or "financials" => "reports",
                "audit" or "security" => "audit-security",
                "integrations" or "erp" or "settings" => "integrations",
                "notifications" or "help" => "notifications",
                _ => token.Trim().ToLowerInvariant()
            };
        }

        private static string? ResolveQuotaDimension(string moduleKey)
        {
            return moduleKey switch
            {
                "tickets" => "tickets",
                "calls" => "calls",
                "catalog" => "storage_mb",
                _ => null
            };
        }

        private static DateTime ResolvePeriodEnd(DateTime periodStart, string? billingCycle)
        {
            return string.Equals(billingCycle, "Annual", StringComparison.OrdinalIgnoreCase)
                ? periodStart.AddYears(1)
                : periodStart.AddMonths(1);
        }

        private static SubscriptionUsageMetricDto BuildMetric(
            string dimension,
            string label,
            string unit,
            IEnumerable<UsageEvent> usageEvents,
            decimal allowed,
            SubscriptionPlan plan)
        {
            var used = usageEvents
                .Where(e => string.Equals(e.Dimension, dimension, StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.Quantity);

            var effectiveAllowed = allowed <= 0 ? decimal.MaxValue : allowed;
            var utilization = effectiveAllowed == decimal.MaxValue
                ? 0
                : Math.Round((used / Math.Max(1, effectiveAllowed)) * 100m, 2);

            var thresholdPercent = plan.IsSoftLimit ? 100m + plan.SoftLimitGracePercent : 100m;
            var isExceeded = utilization > thresholdPercent;
            var isNearLimit = utilization >= 80m && !isExceeded;

            return new SubscriptionUsageMetricDto
            {
                Dimension = dimension,
                Label = label,
                Unit = unit,
                UsedQuantity = used,
                AllowedQuantity = allowed,
                UtilizationPercent = utilization,
                IsNearLimit = isNearLimit,
                IsExceeded = isExceeded
            };
        }

        private async Task UpsertUsagePeriodSummariesAsync(
            string tenantName,
            DateTime periodStart,
            DateTime periodEnd,
            IEnumerable<SubscriptionUsageMetricDto> metrics)
        {
            var normalizedTenant = tenantName.Trim();
            foreach (var metric in metrics)
            {
                var existing = await _context.UsagePeriodSummaries.FirstOrDefaultAsync(s =>
                    s.TenantName == normalizedTenant &&
                    s.Dimension == metric.Dimension &&
                    s.PeriodStart == periodStart &&
                    s.PeriodEnd == periodEnd);

                if (existing == null)
                {
                    _context.UsagePeriodSummaries.Add(new UsagePeriodSummary
                    {
                        TenantName = normalizedTenant,
                        Dimension = metric.Dimension,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                        UsedQuantity = metric.UsedQuantity,
                        AllowedQuantity = metric.AllowedQuantity,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.UsedQuantity = metric.UsedQuantity;
                    existing.AllowedQuantity = metric.AllowedQuantity;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
