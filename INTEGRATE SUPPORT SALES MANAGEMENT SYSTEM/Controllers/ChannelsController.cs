using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChannelsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChannelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetInteractions([FromQuery] ChannelType? channel = null)
        {
            var query = _context.Calls.AsNoTracking().AsQueryable();

            if (channel.HasValue)
            {
                var callType = ToCallType(channel.Value);
                query = query.Where(c => c.Type == callType);
            }
            else
            {
                query = query.Where(c => c.Type == CallType.Chat || c.Type == CallType.Email || c.Type == CallType.SocialMedia);
            }

            query = await ApplyChannelScopeAsync(query);

            var rows = await query
                .OrderByDescending(c => c.StartTime)
                .Select(c => new ChannelInteractionDto
                {
                    Id = c.Id,
                    CustomerId = c.CustomerId,
                    AgentId = c.AgentId,
                    Channel = ToChannelType(c.Type),
                    Subject = c.Subject,
                    Notes = c.Notes,
                    Outcome = c.Outcome,
                    IsEscalated = c.IsEscalated,
                    Status = c.Status,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime
                })
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCustomerInteractions(int customerId)
        {
            if (!await CanAccessCustomerAsync(customerId))
            {
                return Forbid();
            }

            var rows = await _context.Calls
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId && (c.Type == CallType.Chat || c.Type == CallType.Email || c.Type == CallType.SocialMedia))
                .OrderByDescending(c => c.StartTime)
                .Select(c => new ChannelInteractionDto
                {
                    Id = c.Id,
                    CustomerId = c.CustomerId,
                    AgentId = c.AgentId,
                    Channel = ToChannelType(c.Type),
                    Subject = c.Subject,
                    Notes = c.Notes,
                    Outcome = c.Outcome,
                    IsEscalated = c.IsEscalated,
                    Status = c.Status,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime
                })
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetInteractionById(int id)
        {
            var row = await _context.Calls
                .AsNoTracking()
                .Where(c => c.Id == id && (c.Type == CallType.Chat || c.Type == CallType.Email || c.Type == CallType.SocialMedia))
                .Select(c => new ChannelInteractionDto
                {
                    Id = c.Id,
                    CustomerId = c.CustomerId,
                    AgentId = c.AgentId,
                    Channel = ToChannelType(c.Type),
                    Subject = c.Subject,
                    Notes = c.Notes,
                    Outcome = c.Outcome,
                    IsEscalated = c.IsEscalated,
                    Status = c.Status,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime
                })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return NotFound(new { message = "Channel interaction not found" });
            }

            if (!await CanAccessInteractionAsync(id))
            {
                return Forbid();
            }

            return Ok(row);
        }

        [HttpPost]
        [Authorize(Roles = "Agent,Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> CreateInteraction([FromBody] CreateChannelInteractionDto createDto)
        {
            if (!await CanCreateInteractionAsync(createDto.CustomerId, createDto.AgentId))
            {
                return Forbid();
            }

            var row = new Call
            {
                CustomerId = createDto.CustomerId,
                AgentId = createDto.AgentId,
                Type = ToCallType(createDto.Channel),
                Subject = createDto.Subject,
                Notes = createDto.Notes,
                Status = CallStatus.InProgress,
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Calls.Add(row);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInteractionById), new { id = row.Id }, new { row.Id });
        }

        [HttpPost("{id}/resolve")]
        [Authorize(Roles = "Agent,Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> ResolveInteraction(int id, [FromBody] ResolveChannelInteractionDto resolveDto)
        {
            if (!await CanAccessInteractionAsync(id))
            {
                return Forbid();
            }

            var row = await _context.Calls.FirstOrDefaultAsync(c => c.Id == id);
            if (row == null)
            {
                return NotFound(new { message = "Channel interaction not found" });
            }

            row.Outcome = resolveDto.Outcome;
            row.IsEscalated = resolveDto.IsEscalated;
            row.Status = CallStatus.Completed;
            row.EndTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Channel interaction resolved successfully" });
        }

        private static CallType ToCallType(ChannelType channelType)
        {
            return channelType switch
            {
                ChannelType.Chat => CallType.Chat,
                ChannelType.Email => CallType.Email,
                ChannelType.SocialMedia => CallType.SocialMedia,
                _ => CallType.Chat
            };
        }

        private static ChannelType ToChannelType(CallType callType)
        {
            return callType switch
            {
                CallType.Chat => ChannelType.Chat,
                CallType.Email => ChannelType.Email,
                CallType.SocialMedia => ChannelType.SocialMedia,
                _ => ChannelType.Chat
            };
        }

        private async Task<IQueryable<Call>> ApplyChannelScopeAsync(IQueryable<Call> query)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return query;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return query.Where(_ => false);
                }

                return query.Where(c => c.Customer != null && c.Customer.UserId == currentUserId.Value);
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            if (!currentBranchId.HasValue)
            {
                return query.Where(_ => false);
            }

            return query.Where(c => c.Agent != null && c.Agent.BranchId == currentBranchId.Value);
        }

        private async Task<bool> CanAccessCustomerAsync(int customerId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.UserId, BranchId = c.User != null ? c.User.BranchId : null })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return false;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId.HasValue && customer.UserId.HasValue && currentUserId.Value == customer.UserId.Value;
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            return currentBranchId.HasValue && customer.BranchId.HasValue && currentBranchId.Value == customer.BranchId.Value;
        }

        private async Task<bool> CanCreateInteractionAsync(int customerId, int agentId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            if (!currentBranchId.HasValue)
            {
                return false;
            }

            var agentBranchId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == agentId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            var customerBranchId = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => c.User != null ? c.User.BranchId : null)
                .FirstOrDefaultAsync();

            if (!agentBranchId.HasValue || !customerBranchId.HasValue ||
                agentBranchId.Value != currentBranchId.Value ||
                customerBranchId.Value != currentBranchId.Value)
            {
                return false;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                var customerUserId = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Id == customerId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();
                return currentUserId.HasValue && customerUserId.HasValue && currentUserId.Value == customerUserId.Value;
            }

            return true;
        }

        private async Task<bool> CanAccessInteractionAsync(int interactionId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var interaction = await _context.Calls
                .AsNoTracking()
                .Where(c => c.Id == interactionId)
                .Select(c => new
                {
                    c.Id,
                    AgentBranchId = c.Agent != null ? c.Agent.BranchId : null,
                    CustomerUserId = c.Customer != null ? c.Customer.UserId : null
                })
                .FirstOrDefaultAsync();

            if (interaction == null)
            {
                return false;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId.HasValue && interaction.CustomerUserId.HasValue && currentUserId.Value == interaction.CustomerUserId.Value;
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            return currentBranchId.HasValue && interaction.AgentBranchId.HasValue && currentBranchId.Value == interaction.AgentBranchId.Value;
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
    }
}
