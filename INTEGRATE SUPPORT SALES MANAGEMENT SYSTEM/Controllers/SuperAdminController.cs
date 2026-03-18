using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SuperAdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IModuleManagementService _moduleManagementService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SuperAdminController> _logger;

        public SuperAdminController(
            IUserService userService,
            IModuleManagementService moduleManagementService,
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<SuperAdminController> logger)
        {
            _userService = userService;
            _moduleManagementService = moduleManagementService;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<SystemStatsDto>> GetStats()
        {
            var stats = await _userService.GetSystemStatsAsync();
            return Ok(stats);
        }

        [HttpGet("audit-logs")]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs([FromServices] INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data.ApplicationDbContext context)
        {
            // Direct context use for simplicity in audit logs retrieval
            var logs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.AuditLogs.OrderByDescending(l => l.Timestamp).Take(50)
            );
            return Ok(logs);
        }

        [HttpPost("users/{id}/promote")]
        public async Task<IActionResult> PromoteUser(int id, [FromBody] UserRole role)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var updateDto = new UpdateUserDto { Role = role };
            var result = await _userService.UpdateUserAsync(id, updateDto);

            await _userService.LogAuditActionAsync(
                "Role Change",
                $"User {user.Email} promoted to {role}",
                int.Parse(User.FindFirst("UserId")?.Value ?? "0")
            );

            return Ok(result);
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto.Role is UserRole.Admin or UserRole.Supervisor or UserRole.Agent)
            {
                // Automation fallback: if no branch selected, use creator branch (if any).
                createUserDto.BranchId ??= await GetCurrentUserBranchIdAsync();
                if (!createUserDto.BranchId.HasValue || createUserDto.BranchId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Branch assignment is required for Admin, Supervisor, and Agent accounts."
                    });
                }
            }

            if (createUserDto.Role != UserRole.Customer)
            {
                var userLimitMessage = await GetUserLimitValidationMessageAsync();
                if (!string.IsNullOrWhiteSpace(userLimitMessage))
                {
                    return BadRequest(new { message = userLimitMessage });
                }
            }

            var existingUser = await _userService.GetUserByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            var creatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Super Administrator";
            var user = await _userService.CreateUserAsync(createUserDto, creatorName);

            await _userService.LogAuditActionAsync(
                "User Created",
                $"New user created: {user.Email} with role {user.Role}. Email notification sent.",
                int.Parse(User.FindFirst("UserId")?.Value ?? "0")
            );

            return CreatedAtAction(nameof(GetAllUsers), new { id = user.Id }, user);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Prevent deleting yourself
            var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (currentUserId == id)
            {
                return BadRequest(new { message = "You cannot delete your own account" });
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return Conflict(new { message = "Failed to delete user" });
            }

            await _userService.LogAuditActionAsync(
                "User Deleted",
                $"User {user.Email} was deactivated",
                currentUserId
            );

            return Ok(new { message = "User successfully deactivated" });
        }

        [HttpGet("users/recent-activity")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetRecentUserActivity()
        {
            var users = await _userService.GetRecentlyActiveUsersAsync(10);
            return Ok(users);
        }

        [HttpGet("module-access")]
        public async Task<ActionResult<ModuleAccessConfigDto>> GetModuleAccess()
        {
            var config = await _moduleManagementService.GetConfigurationAsync();
            return Ok(config);
        }

        [HttpPut("module-access")]
        public async Task<ActionResult<ModuleAccessConfigDto>> UpdateModuleAccess([FromBody] ModuleAccessConfigDto config)
        {
            if (config == null || config.Modules == null || config.Modules.Count == 0)
            {
                return BadRequest(new { message = "Module configuration payload is required." });
            }

            var previous = await _moduleManagementService.GetConfigurationAsync();
            var updated = await _moduleManagementService.UpdateConfigurationAsync(config);
            var impact = BuildModuleDisableImpact(previous, updated);
            var actorUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var notifiedUsers = await SendModuleDisableNotificationsAsync(impact, actorUserId);

            await _userService.LogAuditActionAsync(
                "Module Access Updated",
                $"Super Admin updated module access configuration for {updated.Modules.Count} modules. " +
                $"Disabled actions: {impact.Count}. Users notified: {notifiedUsers}.",
                actorUserId
            );

            return Ok(updated);
        }

        private static List<ModuleDisableImpactItem> BuildModuleDisableImpact(ModuleAccessConfigDto previous, ModuleAccessConfigDto updated)
        {
            var result = new List<ModuleDisableImpactItem>();
            var previousByKey = (previous.Modules ?? new List<ModuleAccessItemDto>())
                .ToDictionary(x => x.ModuleKey.Trim().ToLowerInvariant(), x => x, StringComparer.OrdinalIgnoreCase);
            foreach (var current in updated.Modules ?? new List<ModuleAccessItemDto>())
            {
                var key = current.ModuleKey.Trim().ToLowerInvariant();
                if (!previousByKey.TryGetValue(key, out var before))
                {
                    continue;
                }

                if (before.IsEnabled && !current.IsEnabled)
                {
                    result.Add(new ModuleDisableImpactItem
                    {
                        ModuleKey = key,
                        ModuleName = current.DisplayName,
                        AffectedRoles = new List<UserRole> { UserRole.Admin, UserRole.Supervisor, UserRole.Agent, UserRole.Customer }
                    });
                    continue;
                }

                if (!current.IsEnabled)
                {
                    continue;
                }

                AddRoleRevocation(result, key, current.DisplayName, UserRole.Admin, before.RoleAccess.Admin, current.RoleAccess.Admin);
                AddRoleRevocation(result, key, current.DisplayName, UserRole.Supervisor, before.RoleAccess.Supervisor, current.RoleAccess.Supervisor);
                AddRoleRevocation(result, key, current.DisplayName, UserRole.Agent, before.RoleAccess.Agent, current.RoleAccess.Agent);
                AddRoleRevocation(result, key, current.DisplayName, UserRole.Customer, before.RoleAccess.Customer, current.RoleAccess.Customer);
            }

            return result;
        }

        private static void AddRoleRevocation(
            List<ModuleDisableImpactItem> impacts,
            string moduleKey,
            string moduleName,
            UserRole role,
            bool beforeAllowed,
            bool nowAllowed)
        {
            if (!beforeAllowed || nowAllowed)
            {
                return;
            }

            impacts.Add(new ModuleDisableImpactItem
            {
                ModuleKey = moduleKey,
                ModuleName = moduleName,
                AffectedRoles = new List<UserRole> { role }
            });
        }

        private async Task<int> SendModuleDisableNotificationsAsync(List<ModuleDisableImpactItem> impacts, int actorUserId)
        {
            if (impacts.Count == 0)
            {
                return 0;
            }

            var roleSet = impacts
                .SelectMany(i => i.AffectedRoles)
                .Distinct()
                .ToHashSet();

            var impactedUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && roleSet.Contains(u.Role))
                .ToListAsync();
            if (impactedUsers.Count == 0)
            {
                return 0;
            }

            var supportContact = _configuration["Support:ContactEmail"]?.Trim();
            if (string.IsNullOrWhiteSpace(supportContact))
            {
                supportContact = "support@classicfitpro.com";
            }

            var timestamp = DateTime.UtcNow;
            var notificationsSent = 0;
            foreach (var user in impactedUsers)
            {
                var roleImpacts = impacts
                    .Where(i => i.AffectedRoles.Contains(user.Role))
                    .ToList();
                if (roleImpacts.Count == 0)
                {
                    continue;
                }

                var body = new StringBuilder();
                body.Append($"<p>Hi {user.FirstName},</p>");
                body.Append("<p>The following module access updates were applied by the Super Admin:</p>");
                body.Append("<ul>");
                foreach (var impact in roleImpacts)
                {
                    body.Append($"<li><strong>{impact.ModuleName}</strong> ({impact.ModuleKey}) - Role: {user.Role}</li>");
                }
                body.Append("</ul>");
                body.Append($"<p><strong>Timestamp (UTC):</strong> {timestamp:yyyy-MM-dd HH:mm:ss}</p>");
                body.Append($"<p>If you need assistance, contact: <a href='mailto:{supportContact}'>{supportContact}</a></p>");

                var sent = await _emailService.SendEmailAsync(
                    user.Email,
                    "ClassicFit Pro - Module Access Update",
                    body.ToString());
                if (sent)
                {
                    notificationsSent++;
                    await _userService.LogAuditActionAsync(
                        "Module Access Notification Sent",
                        $"Notified user '{user.Email}' ({user.Role}) for disabled module access: " +
                        $"{string.Join(", ", roleImpacts.Select(x => $"{x.ModuleName} [{x.ModuleKey}]"))}. " +
                        $"Timestamp UTC: {timestamp:yyyy-MM-dd HH:mm:ss}.",
                        actorUserId);
                }
            }

            _logger.LogInformation(
                "Module disable notification process completed. Impacts: {ImpactCount}, Users: {UserCount}, Sent: {SentCount}",
                impacts.Count,
                impactedUsers.Count,
                notificationsSent);

            return notificationsSent;
        }

        private sealed class ModuleDisableImpactItem
        {
            public string ModuleKey { get; set; } = string.Empty;
            public string ModuleName { get; set; } = string.Empty;
            public List<UserRole> AffectedRoles { get; set; } = new();
        }

        private async Task<string?> GetUserLimitValidationMessageAsync()
        {
            var currentSubscription = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            var userLimit = currentSubscription?.SubscriptionPlan?.MaxUsers;
            if (!userLimit.HasValue || userLimit.Value <= 0)
            {
                return null;
            }

            var activeBillableUsers = await _context.Users.CountAsync(u =>
                u.IsActive &&
                u.Role != UserRole.Customer &&
                u.Role != UserRole.SuperAdmin);

            if (activeBillableUsers >= userLimit.Value)
            {
                return $"User limit reached for current subscription plan. Allowed: {userLimit.Value}.";
            }

            return null;
        }

        private async Task<int?> GetCurrentUserBranchIdAsync()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }
    }
}

