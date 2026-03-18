using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CallsController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly IEntitlementService _entitlementService;
        private readonly ApplicationDbContext _context;
        
        public CallsController(ICallService callService, IEntitlementService entitlementService, ApplicationDbContext context)
        {
            _callService = callService;
            _entitlementService = entitlementService;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllCalls()
        {
            var calls = await _callService.GetAllCallsAsync();
            var scopedCalls = await ApplyCallScopeAsync(calls);
            return Ok(scopedCalls);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCall(int id)
        {
            var call = await _callService.GetCallByIdAsync(id);
            if (call == null)
            {
                return NotFound(new { message = "Call not found" });
            }

            var scopedCalls = await ApplyCallScopeAsync(new[] { call });
            var scopedCall = scopedCalls.FirstOrDefault();
            if (scopedCall == null)
            {
                return Forbid();
            }

            return Ok(scopedCall);
        }
        
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetCallsByAgent(int agentId)
        {
            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                var targetBranchId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == agentId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
                if (!currentBranchId.HasValue || !targetBranchId.HasValue || currentBranchId.Value != targetBranchId.Value)
                {
                    return Forbid();
                }
            }

            var calls = await _callService.GetCallsByAgentAsync(agentId);
            var scopedCalls = await ApplyCallScopeAsync(calls);
            return Ok(scopedCalls);
        }
        
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCallsByCustomer(int customerId)
        {
            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                var customerUserId = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == customerId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();
                if (!currentUserId.HasValue || !customerUserId.HasValue || currentUserId.Value != customerUserId.Value)
                {
                    return Forbid();
                }
            }
            else if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                var customerBranchId = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == customerId)
                    .Select(c => c.User != null ? c.User.BranchId : null)
                    .FirstOrDefaultAsync();
                if (!currentBranchId.HasValue || !customerBranchId.HasValue || currentBranchId.Value != customerBranchId.Value)
                {
                    return Forbid();
                }
            }

            var calls = await _callService.GetCallsByCustomerAsync(customerId);
            var scopedCalls = await ApplyCallScopeAsync(calls);
            return Ok(scopedCalls);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateCall([FromBody] CreateCallDto createCallDto)
        {
            var role = ResolveCurrentRole();
            if (role.HasValue)
            {
                var entitlement = await _entitlementService.EvaluateModuleAccessAsync(role.Value, "calls");
                if (!entitlement.IsVisible)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = entitlement.Message, reasonCode = entitlement.ReasonCode });
                }
            }

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Forbid();
                }

                var agentBranchId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == createCallDto.AgentId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
                var customerBranchId = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == createCallDto.CustomerId)
                    .Select(c => c.User != null ? c.User.BranchId : null)
                    .FirstOrDefaultAsync();

                if (!agentBranchId.HasValue || agentBranchId.Value != currentBranchId.Value ||
                    !customerBranchId.HasValue || customerBranchId.Value != currentBranchId.Value)
                {
                    return Forbid();
                }

                if (User.IsInRole("Customer"))
                {
                    var currentUserId = GetCurrentUserId();
                    var customerUserId = await _context.Customers
                        .AsNoTracking()
                        .Where(c => c.Id == createCallDto.CustomerId)
                        .Select(c => c.UserId)
                        .FirstOrDefaultAsync();
                    if (!currentUserId.HasValue || !customerUserId.HasValue || currentUserId.Value != customerUserId.Value)
                    {
                        return Forbid();
                    }
                }
            }

            var call = await _callService.CreateCallAsync(createCallDto);
            await _entitlementService.RecordUsageAsync("calls", 1m, "count", "call", call.Id);
            return CreatedAtAction(nameof(GetCall), new { id = call.Id }, call);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCall(int id, [FromBody] UpdateCallDto updateCallDto)
        {
            var call = await _callService.UpdateCallAsync(id, updateCallDto);
            if (call == null)
            {
                return NotFound(new { message = "Call not found" });
            }

            return Ok(call);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCall(int id)
        {
            var result = await _callService.DeleteCallAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Call not found" });
            }

            return NoContent();
        }
        
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartCall(int id)
        {
            var result = await _callService.StartCallAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Call not found" });
            }

            return Ok(new { message = "Call started successfully" });
        }
        
        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndCall(int id)
        {
            var result = await _callService.EndCallAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Call not found" });
            }

            return Ok(new { message = "Call ended successfully" });
        }
        
        [HttpGet("summary/{agentId}")]
        public async Task<IActionResult> GetCallSummary(int agentId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var summary = await _callService.GetCallSummaryAsync(agentId, startDate, endDate);
            return Ok(summary);
        }

        private UserRole? ResolveCurrentRole()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentUserBranchIdAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }

        private async Task<IEnumerable<CallDto>> ApplyCallScopeAsync(IEnumerable<CallDto> calls)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return calls;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Enumerable.Empty<CallDto>();
                }

                return calls.Where(c => c.Customer?.UserId == currentUserId.Value);
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            if (!currentBranchId.HasValue)
            {
                return Enumerable.Empty<CallDto>();
            }

            return calls.Where(c => c.Agent?.BranchId == currentBranchId.Value);
        }
    }
}
