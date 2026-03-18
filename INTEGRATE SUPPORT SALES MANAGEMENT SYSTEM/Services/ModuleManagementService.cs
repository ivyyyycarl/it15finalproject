using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class ModuleManagementService : IModuleManagementService
    {
        private readonly ApplicationDbContext _context;

        private static readonly List<ModuleAccessItemDto> DefaultModules =
            new()
        {
            new()
            {
                ModuleKey = "dashboard",
                DisplayName = "Dashboard & Command Center",
                Description = "Overview pages and command-center widgets per actor role.",
                Category = "Core",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = true
                }
            },
            new()
            {
                ModuleKey = "iam",
                DisplayName = "User & Role Management",
                Description = "Identity governance, user lifecycle, and role administration.",
                Category = "Security",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = false,
                    Agent = false,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "catalog",
                DisplayName = "Product Catalog",
                Description = "Catalog management, pricing, and product visibility.",
                Category = "Inventory",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = false,
                    Agent = false,
                    Customer = true
                }
            },
            new()
            {
                ModuleKey = "orders",
                DisplayName = "Orders & Returns",
                Description = "Order processing, status tracking, delivery, and returns workflows.",
                Category = "Operations",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = true
                }
            },
            new()
            {
                ModuleKey = "tickets",
                DisplayName = "Support Tickets",
                Description = "Ticket creation, assignment, escalation, and issue resolution.",
                Category = "Service",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = true
                }
            },
            new()
            {
                ModuleKey = "calls",
                DisplayName = "Call Logs & Interaction History",
                Description = "Call activity, contact history, and communication performance.",
                Category = "Service",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "customers",
                DisplayName = "Customer Profiles",
                Description = "Customer profile search, account context, and timeline views.",
                Category = "CRM",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "reports",
                DisplayName = "Analytics & Financial Reports",
                Description = "Business metrics, financial insights, and custom reporting.",
                Category = "Analytics",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = false,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "audit-security",
                DisplayName = "Audit & Security",
                Description = "Audit trail visibility, alerts, and security monitoring controls.",
                Category = "Security",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = false,
                    Supervisor = false,
                    Agent = false,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "integrations",
                DisplayName = "Integrations & ERP Connections",
                Description = "External system integrations and ERP connectivity management.",
                Category = "Infrastructure",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = false,
                    Supervisor = false,
                    Agent = false,
                    Customer = false
                }
            },
            new()
            {
                ModuleKey = "notifications",
                DisplayName = "Alerts & Notifications",
                Description = "In-app notifications and alert-center operations.",
                Category = "Communication",
                IsEnabled = true,
                RoleAccess = new RoleAccessDto
                {
                    SuperAdmin = true,
                    Admin = true,
                    Supervisor = true,
                    Agent = true,
                    Customer = true
                }
            }
        };

        public ModuleManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<ModuleAccessConfigDto> GetConfigurationAsync()
        {
            return GetConfigurationInternalAsync();
        }

        public async Task<ModuleAccessConfigDto> UpdateConfigurationAsync(ModuleAccessConfigDto config)
        {
            await EnsureSeedDataAsync();

            var normalized = CloneConfig(config ?? new ModuleAccessConfigDto());
            foreach (var module in normalized.Modules)
            {
                module.RoleAccess.SuperAdmin = true;
                var moduleKey = module.ModuleKey.Trim().ToLowerInvariant();
                var definition = await _context.ModuleDefinitions
                    .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey);

                if (definition == null)
                {
                    definition = new ModuleDefinition
                    {
                        ModuleKey = moduleKey,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ModuleDefinitions.Add(definition);
                }

                definition.DisplayName = string.IsNullOrWhiteSpace(module.DisplayName) ? moduleKey : module.DisplayName;
                definition.Description = module.Description;
                definition.Category = string.IsNullOrWhiteSpace(module.Category) ? "General" : module.Category;
                definition.IsActive = module.IsEnabled;
                definition.AllowAdmin = module.RoleAccess.Admin;
                definition.AllowSupervisor = module.RoleAccess.Supervisor;
                definition.AllowAgent = module.RoleAccess.Agent;
                definition.AllowCustomer = module.RoleAccess.Customer;
                definition.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await EnsurePlanEntitlementsCoverageAsync();
            return await GetConfigurationInternalAsync();
        }

        private async Task<ModuleAccessConfigDto> GetConfigurationInternalAsync()
        {
            await EnsureSeedDataAsync();

            var modules = await _context.ModuleDefinitions
                .AsNoTracking()
                .OrderBy(m => m.Category)
                .ThenBy(m => m.DisplayName)
                .Select(m => new ModuleAccessItemDto
                {
                    ModuleKey = m.ModuleKey,
                    DisplayName = m.DisplayName,
                    Description = m.Description ?? string.Empty,
                    Category = m.Category,
                    IsEnabled = m.IsActive,
                    RoleAccess = new RoleAccessDto
                    {
                        SuperAdmin = true,
                        Admin = m.AllowAdmin,
                        Supervisor = m.AllowSupervisor,
                        Agent = m.AllowAgent,
                        Customer = m.AllowCustomer
                    }
                })
                .ToListAsync();

            return new ModuleAccessConfigDto { Modules = modules };
        }

        private async Task EnsureSeedDataAsync()
        {
            if (!await _context.ModuleDefinitions.AnyAsync())
            {
                foreach (var module in DefaultModules)
                {
                    _context.ModuleDefinitions.Add(new ModuleDefinition
                    {
                        ModuleKey = module.ModuleKey.ToLowerInvariant(),
                        DisplayName = module.DisplayName,
                        Description = module.Description,
                        Category = module.Category,
                        IsActive = module.IsEnabled,
                        AllowAdmin = module.RoleAccess.Admin,
                        AllowSupervisor = module.RoleAccess.Supervisor,
                        AllowAgent = module.RoleAccess.Agent,
                        AllowCustomer = module.RoleAccess.Customer,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }

            await EnsurePlanEntitlementsCoverageAsync();
        }

        private async Task EnsurePlanEntitlementsCoverageAsync()
        {
            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .ToListAsync();
            if (plans.Count == 0)
            {
                return;
            }

            var modules = await _context.ModuleDefinitions
                .AsNoTracking()
                .ToListAsync();
            if (modules.Count == 0)
            {
                return;
            }

            var existing = await _context.PlanModuleEntitlements
                .AsNoTracking()
                .Select(e => new { e.SubscriptionPlanId, e.ModuleDefinitionId })
                .ToListAsync();
            var existingKeys = existing
                .Select(e => $"{e.SubscriptionPlanId}:{e.ModuleDefinitionId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<PlanModuleEntitlement>();
            foreach (var plan in plans)
            {
                var included = ParseIncludedModuleKeys(plan.IncludedModulesCsv);
                foreach (var module in modules)
                {
                    var key = $"{plan.Id}:{module.Id}";
                    if (existingKeys.Contains(key))
                    {
                        continue;
                    }

                    var isIncluded = included.Count == 0 || included.Contains("all") || included.Contains(module.ModuleKey);
                    toAdd.Add(new PlanModuleEntitlement
                    {
                        SubscriptionPlanId = plan.Id,
                        ModuleDefinitionId = module.Id,
                        IsIncluded = isIncluded,
                        AllowAdmin = module.AllowAdmin,
                        AllowSupervisor = module.AllowSupervisor,
                        AllowAgent = module.AllowAgent,
                        AllowCustomer = module.AllowCustomer,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                _context.PlanModuleEntitlements.AddRange(toAdd);
                await _context.SaveChangesAsync();
            }
        }

        private static HashSet<string> ParseIncludedModuleKeys(string? includedModulesCsv)
        {
            if (string.IsNullOrWhiteSpace(includedModulesCsv))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return includedModulesCsv
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim().ToLowerInvariant() switch
                {
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
                })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static ModuleAccessConfigDto CloneConfig(ModuleAccessConfigDto source)
        {
            var safeSource = source ?? new ModuleAccessConfigDto();
            var safeModules = safeSource.Modules ?? new List<ModuleAccessItemDto>();

            return new ModuleAccessConfigDto
            {
                Modules = safeModules
                    .Where(m => m != null)
                    .Select(m => new ModuleAccessItemDto
                {
                    ModuleKey = m.ModuleKey ?? string.Empty,
                    DisplayName = m.DisplayName ?? string.Empty,
                    Description = m.Description ?? string.Empty,
                    Category = string.IsNullOrWhiteSpace(m.Category) ? "General" : m.Category,
                    IsEnabled = m.IsEnabled,
                    RoleAccess = new RoleAccessDto
                    {
                        SuperAdmin = m.RoleAccess?.SuperAdmin ?? true,
                        Admin = m.RoleAccess?.Admin ?? false,
                        Supervisor = m.RoleAccess?.Supervisor ?? false,
                        Agent = m.RoleAccess?.Agent ?? false,
                        Customer = m.RoleAccess?.Customer ?? false
                    }
                }).ToList()
            };
        }
    }
}
