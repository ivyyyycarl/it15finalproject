using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public static class ModuleVisibilityHelper
    {
        public static bool IsModuleVisible(
            string moduleKey,
            UserRole role,
            ModuleAccessConfig? moduleAccessConfig,
            TenantSubscriptionModel? tenantSubscription)
        {
            return IsAllowedForRole(moduleKey, role, moduleAccessConfig) &&
                   IsAllowedBySubscription(moduleKey, tenantSubscription);
        }

        public static bool IsAllowedForRole(string moduleKey, UserRole role, ModuleAccessConfig? moduleAccessConfig)
        {
            if (role == UserRole.SuperAdmin)
            {
                return true;
            }

            var module = moduleAccessConfig?.Modules.FirstOrDefault(m =>
                string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));

            if (module == null)
            {
                // Fail-open to avoid blank nav when config API is temporarily unavailable.
                return true;
            }

            if (!module.IsEnabled)
            {
                return false;
            }

            return role switch
            {
                UserRole.Admin => module.RoleAccess.Admin,
                UserRole.Supervisor => module.RoleAccess.Supervisor,
                UserRole.Agent => module.RoleAccess.Agent,
                UserRole.Customer => module.RoleAccess.Customer,
                _ => false
            };
        }

        public static bool IsAllowedBySubscription(string moduleKey, TenantSubscriptionModel? tenantSubscription)
        {
            if (tenantSubscription == null || string.IsNullOrWhiteSpace(tenantSubscription.IncludedModulesCsv))
            {
                return true;
            }

            var rawTokens = tenantSubscription.IncludedModulesCsv
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

        public static string NormalizeIncludedModuleToken(string token)
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
    }
}
