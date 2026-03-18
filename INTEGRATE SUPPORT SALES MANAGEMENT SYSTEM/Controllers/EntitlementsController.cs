using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/entitlements")]
    public class EntitlementsController : ControllerBase
    {
        private readonly IEntitlementService _entitlementService;

        public EntitlementsController(IEntitlementService entitlementService)
        {
            _entitlementService = entitlementService;
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            var role = ResolveUserRole();
            if (!role.HasValue)
            {
                return Forbid();
            }

            var modules = await _entitlementService.GetModuleAccessMapAsync(role.Value);
            return Ok(modules);
        }

        [HttpGet("modules/{moduleKey}")]
        public async Task<IActionResult> GetModule(string moduleKey)
        {
            var role = ResolveUserRole();
            if (!role.HasValue)
            {
                return Forbid();
            }

            var result = await _entitlementService.EvaluateModuleAccessAsync(role.Value, moduleKey);
            return Ok(result);
        }

        [HttpGet("subscription-usage")]
        public async Task<IActionResult> GetSubscriptionUsage()
        {
            var usage = await _entitlementService.GetSubscriptionUsageOverviewAsync();
            if (usage == null)
            {
                return NotFound(new { message = "No active subscription usage context found." });
            }

            return Ok(usage);
        }

        private UserRole? ResolveUserRole()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }
    }
}
