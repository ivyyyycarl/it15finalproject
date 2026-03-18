using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IDataChangeNotifier _notifier;
        private readonly ApplicationDbContext _context;
        private readonly IEntitlementService _entitlementService;
        
        public TicketsController(
            ITicketService ticketService,
            IDataChangeNotifier notifier,
            ApplicationDbContext context,
            IEntitlementService entitlementService)
        {
            _ticketService = ticketService;
            _notifier = notifier;
            _context = context;
            _entitlementService = entitlementService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllTickets()
        {
            var query = _context.Tickets.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CustomerId = t.CustomerId,
                    AssignedAgentId = t.AssignedAgentId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    Resolution = t.Resolution,
                    RelatedCallId = t.RelatedCallId,
                    CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : string.Empty,
                    AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName : string.Empty,
                    CommentCount = t.Comments.Count()
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var isCustomer = ResolveCurrentRole() == UserRole.Customer;
            var query = await ApplyBranchScopeAsync(_context.Tickets.AsNoTracking());
            var ticket = await query
                .Where(t => t.Id == id)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CustomerId = t.CustomerId,
                    AssignedAgentId = t.AssignedAgentId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    Resolution = t.Resolution,
                    RelatedCallId = t.RelatedCallId,
                    CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : string.Empty,
                    AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName : string.Empty,
                    CommentCount = t.Comments.Count(),
                    Comments = t.Comments
                        .Where(c => !isCustomer || !c.IsInternal)
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new TicketCommentDto
                        {
                            Id = c.Id,
                            TicketId = c.TicketId,
                            UserId = c.UserId,
                            User = c.User != null
                                ? new UserDto
                                {
                                    Id = c.User.Id,
                                    FirstName = c.User.FirstName,
                                    LastName = c.User.LastName,
                                    Email = c.User.Email
                                }
                                : null,
                            AuthorName = c.User != null
                                ? c.User.FirstName + " " + c.User.LastName
                                : string.Empty,
                            Comment = c.Comment,
                            IsInternal = c.IsInternal,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            return Ok(ticket);
        }
        
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetTicketsByAgent(int agentId)
        {
            var query = _context.Tickets.AsNoTracking().Where(t => t.AssignedAgentId == agentId);
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CustomerId = t.CustomerId,
                    AssignedAgentId = t.AssignedAgentId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    Resolution = t.Resolution,
                    RelatedCallId = t.RelatedCallId,
                    CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : string.Empty,
                    AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName : string.Empty,
                    CommentCount = t.Comments.Count()
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetTicketsByCustomer(int customerId)
        {
            var query = _context.Tickets.AsNoTracking().Where(t => t.CustomerId == customerId);
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CustomerId = t.CustomerId,
                    AssignedAgentId = t.AssignedAgentId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    Resolution = t.Resolution,
                    RelatedCallId = t.RelatedCallId,
                    CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : string.Empty,
                    AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.FirstName + " " + t.AssignedAgent.LastName : string.Empty,
                    CommentCount = t.Comments.Count()
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto createTicketDto)
        {
            var role = ResolveCurrentRole();
            if (role.HasValue)
            {
                var entitlement = await _entitlementService.EvaluateModuleAccessAsync(role.Value, "tickets");
                if (!entitlement.IsVisible)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = entitlement.Message, reasonCode = entitlement.ReasonCode });
                }
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Forbid();
            }

            createTicketDto.CreatedByUserId = currentUserId.Value;

            if (!IsSuperAdmin())
            {
                if (!createTicketDto.AssignedAgentId.HasValue)
                {
                    createTicketDto.AssignedAgentId = currentUserId.Value;
                }

                var currentBranchId = await GetCurrentBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Forbid();
                }

                var customerBranchId = await _context.Customers
                    .Where(c => c.Id == createTicketDto.CustomerId)
                    .Select(c => c.User != null ? c.User.BranchId : null)
                    .FirstOrDefaultAsync();

                if (!customerBranchId.HasValue || customerBranchId.Value != currentBranchId.Value)
                {
                    return Forbid();
                }

                if (createTicketDto.AssignedAgentId.HasValue)
                {
                    var assignedAgentBranchId = await _context.Users
                        .Where(u => u.Id == createTicketDto.AssignedAgentId.Value && u.IsActive)
                        .Select(u => u.BranchId)
                        .FirstOrDefaultAsync();

                    if (!assignedAgentBranchId.HasValue || assignedAgentBranchId.Value != currentBranchId.Value)
                    {
                        return Forbid();
                    }
                }
            }

            var ticket = await _ticketService.CreateTicketAsync(createTicketDto);
            await _entitlementService.RecordUsageAsync("tickets", 1m, "count", "ticket", ticket.Id);
            await _notifier.NotifyDataChanged("Ticket", "Created");
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketDto updateTicketDto)
        {
            if (!await CanAccessTicketAsync(id))
            {
                return NotFound(new { message = "Ticket not found" });
            }

            if (!IsSuperAdmin() && updateTicketDto.AssignedAgentId.HasValue)
            {
                var currentBranchId = await GetCurrentBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Forbid();
                }

                var assignedAgentBranchId = await _context.Users
                    .Where(u => u.Id == updateTicketDto.AssignedAgentId.Value && u.IsActive)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();

                if (!assignedAgentBranchId.HasValue || assignedAgentBranchId.Value != currentBranchId.Value)
                {
                    return Forbid();
                }
            }

            var ticket = await _ticketService.UpdateTicketAsync(id, updateTicketDto);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            await _notifier.NotifyDataChanged("Ticket", "Updated");
            return Ok(ticket);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            if (!await CanAccessTicketAsync(id))
            {
                return NotFound(new { message = "Ticket not found" });
            }

            var result = await _ticketService.DeleteTicketAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            await _notifier.NotifyDataChanged("Ticket", "Deleted");
            return NoContent();
        }
        
        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketDto assignTicketDto)
        {
            if (!await CanAccessTicketAsync(id))
            {
                return NotFound(new { message = "Ticket not found" });
            }

            if (!IsSuperAdmin())
            {
                var currentBranchId = await GetCurrentBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Forbid();
                }

                var assignedAgentBranchId = await _context.Users
                    .Where(u => u.Id == assignTicketDto.AgentId && u.IsActive)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();

                if (!assignedAgentBranchId.HasValue || assignedAgentBranchId.Value != currentBranchId.Value)
                {
                    return Forbid();
                }
            }

            var result = await _ticketService.AssignTicketAsync(id, assignTicketDto.AgentId);
            if (!result)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            await _notifier.NotifyDataChanged("Ticket", "Assigned");
            return Ok(new { message = "Ticket assigned successfully" });
        }
        
        [HttpPost("{id}/escalate")]
        public async Task<IActionResult> EscalateTicket(int id)
        {
            if (!await CanAccessTicketAsync(id))
            {
                return NotFound(new { message = "Ticket not found" });
            }

            var result = await _ticketService.EscalateTicketAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Ticket not found" });
            }

            await _notifier.NotifyDataChanged("Ticket", "Escalated");
            return Ok(new { message = "Ticket escalated successfully" });
        }

        [HttpPost("backfill-assignments")]
        [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> BackfillAssignments()
        {
            if (!IsSuperAdmin())
            {
                return Forbid();
            }

            var assignedCount = await _ticketService.BackfillUnassignedTicketsAsync();
            if (assignedCount > 0)
            {
                await _notifier.NotifyDataChanged("Ticket", "Backfilled");
            }

            return Ok(new { assignedCount });
        }
        
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateTicketCommentDto createCommentDto)
        {
            if (!await CanAccessTicketAsync(id))
            {
                return NotFound(new { message = "Ticket not found" });
            }

            createCommentDto.TicketId = id;
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "User context is invalid." });
            }

            var comment = await _ticketService.AddCommentAsync(createCommentDto, currentUserId.Value);
            return Ok(comment);
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string? GetCurrentUserEmail()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        }

        private UserRole? ResolveCurrentRole()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }

        private async Task<IQueryable<Ticket>> ApplyBranchScopeAsync(IQueryable<Ticket> query)
        {
            if (IsSuperAdmin())
            {
                return query;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return query.Where(_ => false);
            }

            var currentRole = ResolveCurrentRole();
            if (currentRole == UserRole.Customer)
            {
                // Customers can only access their own tickets regardless of branch assignment.
                var currentEmail = GetCurrentUserEmail();
                if (!string.IsNullOrWhiteSpace(currentEmail))
                {
                    return query.Where(t =>
                        t.Customer != null &&
                        (t.Customer.UserId == currentUserId.Value ||
                         (t.Customer.UserId == null && t.Customer.Email == currentEmail)));
                }

                return query.Where(t => t.Customer != null && t.Customer.UserId == currentUserId.Value);
            }

            var branchId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (!branchId.HasValue)
            {
                return query.Where(_ => false);
            }

            return query.Where(t =>
                (t.AssignedAgent != null && t.AssignedAgent.BranchId == branchId.Value) ||
                (t.CreatedByAgent != null && t.CreatedByAgent.BranchId == branchId.Value) ||
                (t.Customer != null && t.Customer.User != null && t.Customer.User.BranchId == branchId.Value));
        }

        private async Task<int?> GetCurrentBranchIdAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return null;
            }

            return await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> CanAccessTicketAsync(int ticketId)
        {
            var scopedQuery = await ApplyBranchScopeAsync(_context.Tickets.AsNoTracking());
            return await scopedQuery.AnyAsync(t => t.Id == ticketId);
        }
    }
    
    public class AssignTicketDto
    {
        public int AgentId { get; set; }
    }
}
