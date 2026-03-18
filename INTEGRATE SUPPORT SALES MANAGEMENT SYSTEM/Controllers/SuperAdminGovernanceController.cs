using System.Security.Claims;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/superadmin/governance")]
    public class SuperAdminGovernanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public SuperAdminGovernanceController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpGet("subscription/plans")]
        public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.MonthlyPrice)
                .ThenBy(p => p.Name)
                .Select(p => ToPlanDto(p))
                .ToListAsync();

            return Ok(plans);
        }

        [HttpPost("subscription/plans")]
        public async Task<ActionResult<SubscriptionPlanDto>> CreatePlan([FromBody] UpsertSubscriptionPlanRequest request)
        {
            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var exists = await _context.SubscriptionPlans.AnyAsync(p => p.Code == normalizedCode);
            if (exists)
            {
                return Conflict(new { message = $"Subscription plan code '{normalizedCode}' already exists." });
            }

            var plan = new SubscriptionPlan
            {
                Name = request.Name.Trim(),
                Code = normalizedCode,
                Description = request.Description?.Trim(),
                MonthlyPrice = request.MonthlyPrice,
                AnnualPrice = request.AnnualPrice,
                MaxUsers = request.MaxUsers,
                MaxBranches = request.MaxBranches,
                MaxTicketsPerMonth = request.MaxTicketsPerMonth,
                MaxCallLogsPerMonth = request.MaxCallLogsPerMonth,
                MaxStorageMb = request.MaxStorageMb,
                IsSoftLimit = request.IsSoftLimit,
                SoftLimitGracePercent = request.SoftLimitGracePercent,
                IncludedModulesCsv = request.IncludedModulesCsv?.Trim() ?? string.Empty,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            await LogAuditAsync("Subscription Plan Created", $"Created plan '{plan.Name}' ({plan.Code}).");
            return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, ToPlanDto(plan));
        }

        [HttpPut("subscription/plans/{id:int}")]
        public async Task<ActionResult<SubscriptionPlanDto>> UpdatePlan(int id, [FromBody] UpsertSubscriptionPlanRequest request)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null)
            {
                return NotFound(new { message = "Subscription plan not found." });
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var duplicateCode = await _context.SubscriptionPlans.AnyAsync(p => p.Id != id && p.Code == normalizedCode);
            if (duplicateCode)
            {
                return Conflict(new { message = $"Subscription plan code '{normalizedCode}' already exists." });
            }

            plan.Name = request.Name.Trim();
            plan.Code = normalizedCode;
            plan.Description = request.Description?.Trim();
            plan.MonthlyPrice = request.MonthlyPrice;
            plan.AnnualPrice = request.AnnualPrice;
            plan.MaxUsers = request.MaxUsers;
            plan.MaxBranches = request.MaxBranches;
            plan.MaxTicketsPerMonth = request.MaxTicketsPerMonth;
            plan.MaxCallLogsPerMonth = request.MaxCallLogsPerMonth;
            plan.MaxStorageMb = request.MaxStorageMb;
            plan.IsSoftLimit = request.IsSoftLimit;
            plan.SoftLimitGracePercent = request.SoftLimitGracePercent;
            plan.IncludedModulesCsv = request.IncludedModulesCsv?.Trim() ?? string.Empty;
            plan.IsActive = request.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogAuditAsync("Subscription Plan Updated", $"Updated plan '{plan.Name}' ({plan.Code}).");

            return Ok(ToPlanDto(plan));
        }

        [HttpGet("subscription/plans/{id:int}/modules")]
        public async Task<ActionResult<List<PlanModuleEntitlementDto>>> GetPlanModuleEntitlements(int id)
        {
            var plan = await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null)
            {
                return NotFound(new { message = "Subscription plan not found." });
            }

            var modules = await _context.ModuleDefinitions
                .AsNoTracking()
                .OrderBy(m => m.Category)
                .ThenBy(m => m.DisplayName)
                .ToListAsync();

            var existing = await _context.PlanModuleEntitlements
                .Where(e => e.SubscriptionPlanId == id)
                .ToListAsync();

            var existingByModuleId = existing.ToDictionary(e => e.ModuleDefinitionId);
            var items = new List<PlanModuleEntitlementDto>();
            foreach (var module in modules)
            {
                existingByModuleId.TryGetValue(module.Id, out var entitlement);
                items.Add(new PlanModuleEntitlementDto
                {
                    ModuleDefinitionId = module.Id,
                    ModuleKey = module.ModuleKey,
                    DisplayName = module.DisplayName,
                    Category = module.Category,
                    IsIncluded = entitlement?.IsIncluded ?? IsIncludedByPlanCsv(plan.IncludedModulesCsv, module.ModuleKey),
                    AllowAdmin = entitlement?.AllowAdmin ?? module.AllowAdmin,
                    AllowSupervisor = entitlement?.AllowSupervisor ?? module.AllowSupervisor,
                    AllowAgent = entitlement?.AllowAgent ?? module.AllowAgent,
                    AllowCustomer = entitlement?.AllowCustomer ?? module.AllowCustomer
                });
            }

            return Ok(items);
        }

        [HttpPut("subscription/plans/{id:int}/modules")]
        public async Task<ActionResult<List<PlanModuleEntitlementDto>>> UpdatePlanModuleEntitlements(
            int id,
            [FromBody] UpdatePlanModuleEntitlementsRequest request)
        {
            if (request?.Modules == null || request.Modules.Count == 0)
            {
                return BadRequest(new { message = "At least one module entitlement is required." });
            }

            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null)
            {
                return NotFound(new { message = "Subscription plan not found." });
            }

            var moduleIds = request.Modules.Select(m => m.ModuleDefinitionId).Distinct().ToList();
            var moduleDefinitions = await _context.ModuleDefinitions
                .Where(m => moduleIds.Contains(m.Id))
                .ToListAsync();
            if (moduleDefinitions.Count != moduleIds.Count)
            {
                return BadRequest(new { message = "One or more module definitions are invalid." });
            }

            var existing = await _context.PlanModuleEntitlements
                .Where(e => e.SubscriptionPlanId == id && moduleIds.Contains(e.ModuleDefinitionId))
                .ToListAsync();

            var existingByModuleId = existing.ToDictionary(e => e.ModuleDefinitionId);
            foreach (var item in request.Modules)
            {
                if (!existingByModuleId.TryGetValue(item.ModuleDefinitionId, out var entitlement))
                {
                    entitlement = new PlanModuleEntitlement
                    {
                        SubscriptionPlanId = id,
                        ModuleDefinitionId = item.ModuleDefinitionId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PlanModuleEntitlements.Add(entitlement);
                }

                entitlement.IsIncluded = item.IsIncluded;
                entitlement.AllowAdmin = item.AllowAdmin;
                entitlement.AllowSupervisor = item.AllowSupervisor;
                entitlement.AllowAgent = item.AllowAgent;
                entitlement.AllowCustomer = item.AllowCustomer;
                entitlement.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await LogAuditAsync(
                "Plan Module Entitlements Updated",
                $"Updated {request.Modules.Count} module entitlement(s) for plan '{plan.Name}' ({plan.Code}).");

            return await GetPlanModuleEntitlements(id);
        }

        [HttpGet("analytics/overview")]
        public async Task<ActionResult<SuperAdminGovernanceAnalyticsDto>> GetAnalyticsOverview()
        {
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, now.Month, 1);
            var periodEnd = periodStart.AddMonths(1);

            var activeSubscriptions = await _context.TenantSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial);

            var monthlyRecurringRevenueEstimate = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                .Select(s => s.BillingCycle == "Annual"
                    ? (s.SubscriptionPlan != null ? s.SubscriptionPlan.AnnualPrice / 12m : 0m)
                    : (s.SubscriptionPlan != null ? s.SubscriptionPlan.MonthlyPrice : 0m))
                .DefaultIfEmpty(0m)
                .SumAsync();

            var revenueCollectedCurrentPeriod = await _context.SubscriptionInvoiceRecords
                .Where(i => i.PaidAt.HasValue &&
                            i.PaidAt.Value >= periodStart &&
                            i.PaidAt.Value < periodEnd &&
                            i.Status == "paid")
                .Select(i => i.AmountPaid)
                .DefaultIfEmpty(0m)
                .SumAsync();

            var planAdoption = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .GroupBy(s => new { PlanName = s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "Unknown", PlanCode = s.SubscriptionPlan != null ? s.SubscriptionPlan.Code : "N/A" })
                .Select(g => new PlanAdoptionItemDto
                {
                    PlanName = g.Key.PlanName,
                    PlanCode = g.Key.PlanCode,
                    TenantCount = g.Count()
                })
                .OrderByDescending(x => x.TenantCount)
                .ToListAsync();

            var moduleUsage = await _context.UsageEvents
                .Where(u => u.OccurredAt >= periodStart && u.OccurredAt < periodEnd)
                .GroupBy(u => u.Dimension)
                .Select(g => new ModuleUsageItemDto
                {
                    Dimension = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ToListAsync();

            return Ok(new SuperAdminGovernanceAnalyticsDto
            {
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRecurringRevenueEstimate = monthlyRecurringRevenueEstimate,
                RevenueCollectedCurrentPeriod = revenueCollectedCurrentPeriod,
                PlanAdoption = planAdoption,
                ModuleUsage = moduleUsage
            });
        }

        [HttpGet("subscription/current")]
        public async Task<ActionResult<TenantSubscriptionDto>> GetCurrentSubscription()
        {
            var current = await _context.TenantSubscriptions
                .Include(x => x.SubscriptionPlan)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (current == null || current.SubscriptionPlan == null)
            {
                return NotFound(new { message = "No active tenant subscription configured." });
            }

            return Ok(ToSubscriptionDto(current));
        }

        [HttpPut("subscription/current")]
        public async Task<ActionResult<TenantSubscriptionDto>> UpdateCurrentSubscription([FromBody] UpdateTenantSubscriptionRequest request)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
            if (plan == null)
            {
                return BadRequest(new { message = "Selected subscription plan is invalid or inactive." });
            }

            var current = await _context.TenantSubscriptions
                .Include(x => x.SubscriptionPlan)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (current == null)
            {
                current = new TenantSubscription
                {
                    TenantName = "ClassicFit",
                    SubscriptionPlanId = plan.Id,
                    Status = request.Status,
                    AutoRenew = request.AutoRenew,
                    BillingCycle = request.BillingCycle,
                    StartsAt = request.StartsAt ?? DateTime.UtcNow,
                    EndsAt = request.EndsAt,
                    NextBillingAt = request.NextBillingAt ?? DateTime.UtcNow.AddMonths(1),
                    CurrentPeriodStart = request.StartsAt ?? DateTime.UtcNow,
                    CurrentPeriodEnd = (request.StartsAt ?? DateTime.UtcNow).AddMonths(request.BillingCycle.Equals("Annual", StringComparison.OrdinalIgnoreCase) ? 12 : 1),
                    UnitPrice = request.BillingCycle.Equals("Annual", StringComparison.OrdinalIgnoreCase) ? plan.AnnualPrice : plan.MonthlyPrice,
                    Currency = "PHP",
                    CreatedAt = DateTime.UtcNow
                };

                _context.TenantSubscriptions.Add(current);
            }
            else
            {
                current.SubscriptionPlanId = plan.Id;
                current.Status = request.Status;
                current.AutoRenew = request.AutoRenew;
                current.BillingCycle = request.BillingCycle;
                current.StartsAt = request.StartsAt ?? current.StartsAt;
                current.EndsAt = request.EndsAt;
                current.NextBillingAt = request.NextBillingAt ?? current.NextBillingAt;
                current.CurrentPeriodStart = request.StartsAt ?? current.CurrentPeriodStart ?? current.StartsAt;
                current.CurrentPeriodEnd = current.CurrentPeriodStart?.AddMonths(request.BillingCycle.Equals("Annual", StringComparison.OrdinalIgnoreCase) ? 12 : 1);
                current.UnitPrice = request.BillingCycle.Equals("Annual", StringComparison.OrdinalIgnoreCase) ? plan.AnnualPrice : plan.MonthlyPrice;
                current.Currency = "PHP";
                current.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            current = await _context.TenantSubscriptions
                .Include(x => x.SubscriptionPlan)
                .OrderByDescending(x => x.CreatedAt)
                .FirstAsync();

            await LogAuditAsync(
                "Tenant Subscription Updated",
                $"Updated current subscription to plan '{current.SubscriptionPlan?.Name ?? "Unknown"}' with status '{current.Status}'.");

            return Ok(ToSubscriptionDto(current));
        }

        [HttpGet("branches")]
        public async Task<ActionResult<List<BranchDto>>> GetBranches()
        {
            var branches = await _context.Branches
                .Include(b => b.Users)
                .OrderByDescending(b => b.IsActive)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return Ok(branches.Select(ToBranchDto).ToList());
        }

        [HttpPost("branches")]
        public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] UpsertBranchRequest request)
        {
            try
            {
                await EnsureBranchSlotAvailable();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var codeExists = await _context.Branches.AnyAsync(b => b.Code == normalizedCode);
            if (codeExists)
            {
                return Conflict(new { message = $"Branch code '{normalizedCode}' already exists." });
            }

            var branch = new Branch
            {
                Name = request.Name.Trim(),
                Code = normalizedCode,
                AddressLine = request.AddressLine?.Trim(),
                City = request.City?.Trim(),
                Province = request.Province?.Trim(),
                Country = request.Country?.Trim(),
                ZipCode = request.ZipCode?.Trim(),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            await LogAuditAsync("Branch Created", $"Created branch '{branch.Name}' ({branch.Code}).");
            return Ok(ToBranchDto(branch));
        }

        [HttpPut("branches/{id:int}")]
        public async Task<ActionResult<BranchDto>> UpdateBranch(int id, [FromBody] UpsertBranchRequest request)
        {
            var branch = await _context.Branches.Include(b => b.Users).FirstOrDefaultAsync(b => b.Id == id);
            if (branch == null)
            {
                return NotFound(new { message = "Branch not found." });
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var duplicateCode = await _context.Branches.AnyAsync(b => b.Id != id && b.Code == normalizedCode);
            if (duplicateCode)
            {
                return Conflict(new { message = $"Branch code '{normalizedCode}' already exists." });
            }

            branch.Name = request.Name.Trim();
            branch.Code = normalizedCode;
            branch.AddressLine = request.AddressLine?.Trim();
            branch.City = request.City?.Trim();
            branch.Province = request.Province?.Trim();
            branch.Country = request.Country?.Trim();
            branch.ZipCode = request.ZipCode?.Trim();
            branch.IsActive = request.IsActive;
            branch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogAuditAsync("Branch Updated", $"Updated branch '{branch.Name}' ({branch.Code}).");

            return Ok(ToBranchDto(branch));
        }

        [HttpDelete("branches/{id:int}")]
        public async Task<IActionResult> DeactivateBranch(int id)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id);
            if (branch == null)
            {
                return NotFound(new { message = "Branch not found." });
            }

            if (!branch.IsActive)
            {
                return BadRequest(new { message = "Branch is already inactive." });
            }

            var activeBranchCount = await _context.Branches.CountAsync(b => b.IsActive);
            if (activeBranchCount <= 1)
            {
                return BadRequest(new { message = "Cannot deactivate the last active branch." });
            }

            var assignedUsersCount = await _context.Users.CountAsync(u => u.IsActive && u.BranchId == branch.Id);
            if (assignedUsersCount > 0)
            {
                return BadRequest(new { message = $"Cannot deactivate branch '{branch.Name}' while {assignedUsersCount} active user(s) are still assigned. Reassign users first." });
            }

            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogAuditAsync("Branch Deactivated", $"Deactivated branch '{branch.Name}' ({branch.Code}).");
            return Ok(new { message = "Branch deactivated successfully." });
        }

        [HttpPut("users/assign-branch")]
        public async Task<IActionResult> AssignUserBranch([FromBody] AssignUserBranchRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.Role == UserRole.SuperAdmin)
            {
                return BadRequest(new { message = "Super Admin users cannot be assigned to a branch." });
            }

            if (request.BranchId.HasValue)
            {
                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId.Value && b.IsActive);
                if (branch == null)
                {
                    return BadRequest(new { message = "Selected branch is invalid or inactive." });
                }
            }

            user.BranchId = request.BranchId;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogAuditAsync(
                "User Branch Assigned",
                request.BranchId.HasValue
                    ? $"Assigned user '{user.Email}' to branch ID {request.BranchId.Value}."
                    : $"Removed branch assignment for user '{user.Email}'.");

            return Ok(new { message = "User branch assignment updated." });
        }

        private async Task EnsureBranchSlotAvailable()
        {
            var activeBranchCount = await _context.Branches.CountAsync(b => b.IsActive);
            var currentSubscription = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (currentSubscription?.SubscriptionPlan == null)
            {
                return;
            }

            var allowedBranches = currentSubscription.SubscriptionPlan.MaxBranches;
            if (allowedBranches <= 0)
            {
                return; // Unlimited branches for enterprise-style plans.
            }

            if (activeBranchCount >= allowedBranches)
            {
                throw new InvalidOperationException(
                    $"Branch limit reached for plan '{currentSubscription.SubscriptionPlan.Name}'. Allowed: {allowedBranches}.");
            }
        }

        private async Task LogAuditAsync(string action, string description)
        {
            var userIdText = User.FindFirstValue("UserId");
            if (int.TryParse(userIdText, out var userId) && userId > 0)
            {
                await _userService.LogAuditActionAsync(action, description, userId);
            }
        }

        private static SubscriptionPlanDto ToPlanDto(SubscriptionPlan p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            MonthlyPrice = p.MonthlyPrice,
            AnnualPrice = p.AnnualPrice,
            MaxUsers = p.MaxUsers,
            MaxBranches = p.MaxBranches,
            MaxTicketsPerMonth = p.MaxTicketsPerMonth,
            MaxCallLogsPerMonth = p.MaxCallLogsPerMonth,
            MaxStorageMb = p.MaxStorageMb,
            IsSoftLimit = p.IsSoftLimit,
            SoftLimitGracePercent = p.SoftLimitGracePercent,
            IncludedModulesCsv = p.IncludedModulesCsv,
            IsActive = p.IsActive
        };

        private static TenantSubscriptionDto ToSubscriptionDto(TenantSubscription s) => new()
        {
            Id = s.Id,
            TenantName = s.TenantName,
            Status = s.Status,
            StartsAt = s.StartsAt,
            EndsAt = s.EndsAt,
            NextBillingAt = s.NextBillingAt,
            AutoRenew = s.AutoRenew,
            BillingCycle = s.BillingCycle,
            CurrentPeriodStart = s.CurrentPeriodStart,
            CurrentPeriodEnd = s.CurrentPeriodEnd,
            Currency = s.Currency,
            UnitPrice = s.UnitPrice,
            DiscountAmount = s.DiscountAmount,
            TaxAmount = s.TaxAmount,
            LastPaymentStatus = s.LastPaymentStatus,
            LastPaymentAt = s.LastPaymentAt,
            TrialStartsAt = s.TrialStartsAt,
            TrialEndsAt = s.TrialEndsAt,
            CanceledAt = s.CanceledAt,
            CancelReason = s.CancelReason,
            SubscriptionPlanId = s.SubscriptionPlanId,
            PlanName = s.SubscriptionPlan?.Name ?? string.Empty,
            PlanCode = s.SubscriptionPlan?.Code ?? string.Empty,
            MaxUsers = s.SubscriptionPlan?.MaxUsers ?? 0,
            MaxBranches = s.SubscriptionPlan?.MaxBranches ?? 0,
            MaxTicketsPerMonth = s.SubscriptionPlan?.MaxTicketsPerMonth ?? 0,
            MaxCallLogsPerMonth = s.SubscriptionPlan?.MaxCallLogsPerMonth ?? 0,
            MaxStorageMb = s.SubscriptionPlan?.MaxStorageMb ?? 0,
            IncludedModulesCsv = s.SubscriptionPlan?.IncludedModulesCsv ?? string.Empty
        };

        private static BranchDto ToBranchDto(Branch b) => new()
        {
            Id = b.Id,
            Name = b.Name,
            Code = b.Code,
            AddressLine = b.AddressLine,
            City = b.City,
            Province = b.Province,
            Country = b.Country,
            ZipCode = b.ZipCode,
            IsActive = b.IsActive,
            AssignedUsersCount = b.Users?.Count ?? 0
        };

        private static bool IsIncludedByPlanCsv(string? csv, string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return true;
            }

            var tokens = csv
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList();

            if (tokens.Count == 0 || tokens.Contains("all"))
            {
                return true;
            }

            return tokens.Contains(moduleKey.Trim().ToLowerInvariant());
        }
    }
}
