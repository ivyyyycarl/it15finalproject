using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet("user-registrations-trend")]
        public async Task<IActionResult> GetUserRegistrationsTrend()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-30);
            var usersQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= startDate);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                usersQuery = usersQuery.Where(u => u.BranchId == currentBranchId.Value);
            }

            var users = await usersQuery
                .Select(u => u.CreatedAt.Date)
                .ToListAsync();

            var data = Enumerable.Range(0, 31).Select(i =>
            {
                var date = startDate.AddDays(i);
                return new { Date = date, Value = (decimal)users.Count(d => d == date.Date) };
            }).ToList();

            return Ok(data);
        }

        [HttpGet("sales-by-category")]
        public async Task<IActionResult> GetSalesByCategory()
        {
            var query = _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.Product != null);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                query = query.Where(od =>
                    (od.Order.Agent != null && od.Order.Agent.BranchId == currentBranchId.Value) ||
                    (od.Order.Agent == null && od.Order.Customer.User != null && od.Order.Customer.User.BranchId == currentBranchId.Value));
            }

            var data = await query
                .GroupBy(od => od.Product!.Category)
                .Select(g => new { Name = g.Key.ToString(), Value = g.Sum(od => od.TotalPrice) })
                .OrderByDescending(x => x.Value)
                .ToListAsync();


            return Ok(data);
        }

        [HttpGet("ticket-status-distribution")]
        public async Task<IActionResult> GetTicketStatusDistribution()
        {
            var ticketsQuery = _context.Tickets.AsNoTracking().AsQueryable();

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                ticketsQuery = ticketsQuery.Where(t =>
                    (t.AssignedAgent != null && t.AssignedAgent.BranchId == currentBranchId.Value) ||
                    (t.AssignedAgent == null && t.Customer.User != null && t.Customer.User.BranchId == currentBranchId.Value));
            }

            var tickets = await ticketsQuery.ToListAsync();

            var data = tickets
                .GroupBy(t => t.Status.ToString())
                .Select(g => new { Name = g.Key, Value = (decimal)g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();

            return Ok(data);
        }

        [HttpGet("agent-throughput")]
        public async Task<IActionResult> GetAgentThroughput()
        {
            var agentQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Agent && u.IsActive);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                agentQuery = agentQuery.Where(u => u.BranchId == currentBranchId.Value);
            }

            var result = await agentQuery
                .Select(agent => new
                {
                    AgentName = $"{agent.FirstName} {agent.LastName}",
                    TicketsResolved = _context.Tickets.Count(t => t.AssignedAgentId == agent.Id && t.Status == TicketStatus.Closed),
                    CallsHandled = _context.Calls.Count(c => c.AgentId == agent.Id)
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("order-trend")]
        public async Task<IActionResult> GetOrderTrend()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-14);
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= startDate);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                ordersQuery = ordersQuery.Where(o =>
                    (o.Agent != null && o.Agent.BranchId == currentBranchId.Value) ||
                    (o.Agent == null && o.Customer.User != null && o.Customer.User.BranchId == currentBranchId.Value));
            }

            var orders = await ordersQuery
                .Select(o => o.OrderDate.Date)
                .ToListAsync();

            var data = Enumerable.Range(0, 15).Select(i =>
            {
                var date = startDate.AddDays(i);
                return new { Date = date, Value = (decimal)orders.Count(d => d == date.Date) };
            }).ToList();

            return Ok(data);
        }

        [HttpGet("top-selling-products")]
        public async Task<IActionResult> GetTopSellingProducts()
        {
            var query = _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.Product != null);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                query = query.Where(od =>
                    (od.Order.Agent != null && od.Order.Agent.BranchId == currentBranchId.Value) ||
                    (od.Order.Agent == null && od.Order.Customer.User != null && od.Order.Customer.User.BranchId == currentBranchId.Value));
            }

            var data = await query
                .GroupBy(od => od.Product!.Name)
                .Select(g => new { Name = g.Key, Value = (decimal)g.Sum(od => od.Quantity) })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("stock-health")]
        public async Task<IActionResult> GetStockHealth()
        {
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            if (totalProducts == 0)
                return Ok(new { Value = 100.0m });

            var healthyStock = await _context.Products
                .CountAsync(p => p.IsActive && p.StockQuantity > p.MinStockLevel);

            var percentage = Math.Round((decimal)healthyStock / totalProducts * 100, 1);
            return Ok(new { Value = percentage });
        }

        [HttpGet("inventory-valuation")]
        public async Task<IActionResult> GetInventoryValuation()
        {
            var valuation = await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.Price * p.StockQuantity);

            return Ok(new { Value = valuation });
        }

        [HttpGet("sales-trend")]
        public async Task<IActionResult> GetSalesTrend()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-14);
            var orderDetailsQuery = _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate >= startDate);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return Ok(new List<object>());
                }

                orderDetailsQuery = orderDetailsQuery.Where(od =>
                    (od.Order.Agent != null && od.Order.Agent.BranchId == currentBranchId.Value) ||
                    (od.Order.Agent == null && od.Order.Customer.User != null && od.Order.Customer.User.BranchId == currentBranchId.Value));
            }

            var orderDetails = await orderDetailsQuery
                .Select(od => new { Date = od.Order.OrderDate.Date, Amount = od.TotalPrice })
                .ToListAsync();

            var data = Enumerable.Range(0, 15).Select(i =>
            {
                var date = startDate.AddDays(i);
                return new { Date = date, Value = orderDetails.Where(od => od.Date == date).Sum(od => od.Amount) };
            }).ToList();

            return Ok(data);
        }

        [HttpPost("seed-demo-data")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SeedDemoData()
        {
            await DbSeeder.SeedDemoData(_context);
            return Ok(new { Message = "Demo data seeded successfully" });
        }

        [HttpPost("reset-database")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetDatabase()
        {
            await DbSeeder.PurgeDemoData(_context);
            return Ok(new { Message = "Database reset successfully. Demo data removed." });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentUserBranchIdAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }
    }
}
