using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TimelineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TimelineController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<TimelineItemDto>>> GetCustomerTimeline(int customerId)
        {
            if (!await CanAccessCustomerTimelineAsync(customerId))
            {
                return Forbid();
            }

            var timeline = new List<TimelineItemDto>();

            // Get Calls
            var calls = await _context.Calls
                .Where(c => c.CustomerId == customerId)
                .Select(c => new TimelineItemDto
                {
                    Id = c.Id,
                    Type = "Call",
                    Title = string.IsNullOrEmpty(c.Subject) ? "Inbound Call" : c.Subject,
                    Description = c.Notes,
                    Timestamp = c.StartTime,
                    Status = c.Status.ToString(),
                    AgentId = c.AgentId
                })
                .ToListAsync();
            timeline.AddRange(calls);

            // Get Tickets
            var tickets = await _context.Tickets
                .Where(t => t.CustomerId == customerId)
                .Select(t => new TimelineItemDto
                {
                    Id = t.Id,
                    Type = "Ticket",
                    Title = t.Title,
                    Description = t.Description,
                    Timestamp = t.CreatedAt,
                    Status = t.Status.ToString(),
                    AgentId = t.AssignedAgentId ?? 0
                })
                .ToListAsync();
            timeline.AddRange(tickets);

            // Get Orders
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Select(o => new TimelineItemDto
                {
                    Id = o.Id,
                    Type = "Order",
                    Title = $"Order #{o.OrderNumber}",
                    Description = $"Total: ${o.FinalAmount:N2}",
                    Timestamp = o.CreatedAt,
                    Status = o.Status.ToString(),
                    AgentId = o.AgentId ?? 0
                })
                .ToListAsync();
            timeline.AddRange(orders);

            return Ok(timeline.OrderByDescending(t => t.Timestamp));
        }

        private async Task<bool> CanAccessCustomerTimelineAsync(int customerId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Id, c.UserId, BranchId = c.User != null ? c.User.BranchId : null })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return false;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            if (User.IsInRole("Customer"))
            {
                return customer.UserId.HasValue && customer.UserId.Value == currentUserId.Value;
            }

            var currentBranchId = await GetCurrentUserBranchIdAsync();
            return currentBranchId.HasValue && customer.BranchId.HasValue && currentBranchId.Value == customer.BranchId.Value;
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

    public class TimelineItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // Call, Ticket, Order
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AgentId { get; set; }
    }
}
