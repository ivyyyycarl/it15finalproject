using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;

        public CustomersController(ICustomerService customerService, ApplicationDbContext context)
        {
            _customerService = customerService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCustomers()
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);
            var items = await query
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    CreatedByUserId = c.CreatedByUserId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Company = c.Company,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    PostalCode = c.PostalCode,
                    Country = c.Country,
                    Type = c.Type.ToString(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    TotalCalls = c.Calls.Count(),
                    OpenTickets = c.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = c.Orders.Count(),
                    BranchId = c.User != null ? c.User.BranchId : null,
                    BranchName = c.User != null && c.User.Branch != null ? c.User.Branch.Name : null,
                    TotalSpent = c.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetCustomersPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortDir = "asc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _context.Customers.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    (c.Company != null && c.Company.ToLower().Contains(term)));
            }

            var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "email" => isDesc ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
                "company" => isDesc ? query.OrderByDescending(c => c.Company) : query.OrderBy(c => c.Company),
                "createdat" => isDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                "lastname" => isDesc ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName) : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),
                _ => isDesc ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName) : query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Company = c.Company,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    PostalCode = c.PostalCode,
                    Country = c.Country,
                    Type = c.Type.ToString(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    TotalCalls = c.Calls.Count(),
                    OpenTickets = c.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = c.Orders.Count(),
                    BranchId = c.User != null ? c.User.BranchId : null,
                    BranchName = c.User != null && c.User.Branch != null ? c.User.Branch.Name : null,
                    TotalSpent = c.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .ToListAsync();

            return Ok(PagedResultDto<CustomerDto>.Create(items, page, pageSize, totalCount));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var query = await ApplyBranchScopeAsync(_context.Customers.AsNoTracking());
            var customer = await query
                .Where(c => c.Id == id)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    CreatedByUserId = c.CreatedByUserId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Company = c.Company,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    PostalCode = c.PostalCode,
                    Country = c.Country,
                    Type = c.Type.ToString(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    TotalCalls = c.Calls.Count(),
                    OpenTickets = c.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = c.Orders.Count(),
                    BranchId = c.User != null ? c.User.BranchId : null,
                    BranchName = c.User != null && c.User.Branch != null ? c.User.Branch.Name : null,
                    TotalSpent = c.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .FirstOrDefaultAsync();
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            return Ok(customer);
        }

        [HttpGet("{id}/interactions")]
        public async Task<IActionResult> GetCustomerInteractions(int id)
        {
            var scoped = await ApplyBranchScopeAsync(_context.Customers.AsNoTracking());
            var allowed = await scoped.AnyAsync(c => c.Id == id);
            if (!allowed)
            {
                return NotFound(new { message = "Customer not found" });
            }

            var interactions = await _customerService.GetCustomerInteractionHistoryAsync(id);
            return Ok(interactions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto createCustomerDto)
        {
            if (!IsSuperAdmin())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                createCustomerDto.CreatedByUserId = currentUserId.Value;

                if (IsCustomer())
                {
                    // Customer self-onboarding: allow creating only their own customer profile,
                    // even when branch is not yet assigned.
                    if (!createCustomerDto.UserId.HasValue || createCustomerDto.UserId.Value != currentUserId.Value)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    var currentBranchId = await GetCurrentBranchIdAsync();
                    if (!currentBranchId.HasValue)
                    {
                        return Forbid();
                    }

                    if (createCustomerDto.UserId.HasValue)
                    {
                        var linkedUserBranchId = await _context.Users
                            .Where(u => u.Id == createCustomerDto.UserId.Value && u.IsActive)
                            .Select(u => u.BranchId)
                            .FirstOrDefaultAsync();

                        if (!linkedUserBranchId.HasValue || linkedUserBranchId.Value != currentBranchId.Value)
                        {
                            return Forbid();
                        }
                    }
                }
            }

            var existingCustomer = await _customerService.GetCustomerByEmailAsync(createCustomerDto.Email);
            if (existingCustomer != null)
            {
                if (createCustomerDto.UserId.HasValue && existingCustomer.UserId == null)
                {
                    await _customerService.LinkCustomerToUserAsync(existingCustomer.Id, createCustomerDto.UserId.Value);
                    var updated = await _customerService.GetCustomerByIdAsync(existingCustomer.Id);
                    return Ok(updated);
                }

                if (createCustomerDto.UserId.HasValue
                    && existingCustomer.UserId.HasValue
                    && existingCustomer.UserId.Value != createCustomerDto.UserId.Value)
                {
                    return Conflict(new
                    {
                        message = "A customer profile with this email is already linked to another account."
                    });
                }

                return Ok(existingCustomer);
            }

            var customer = await _customerService.CreateCustomerAsync(createCustomerDto);
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto updateCustomerDto)
        {
            var scoped = await ApplyBranchScopeAsync(_context.Customers.AsNoTracking());
            var allowed = await scoped.AnyAsync(c => c.Id == id);
            if (!allowed)
            {
                return NotFound(new { message = "Customer not found" });
            }

            var customer = await _customerService.UpdateCustomerAsync(id, updateCustomerDto);
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            return Ok(customer);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var scoped = await ApplyBranchScopeAsync(_context.Customers.AsNoTracking());
            var allowed = await scoped.AnyAsync(c => c.Id == id);
            if (!allowed)
            {
                return NotFound(new { message = "Customer not found" });
            }

            var result = await _customerService.DeleteCustomerAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Customer not found" });
            }

            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchCustomers([FromQuery] string searchTerm)
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);
            var term = searchTerm?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowered = term.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(lowered) ||
                    c.LastName.ToLower().Contains(lowered) ||
                    c.Email.ToLower().Contains(lowered) ||
                    (c.Company != null && c.Company.ToLower().Contains(lowered)));
            }

            var items = await query
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Take(100)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    CreatedByUserId = c.CreatedByUserId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Company = c.Company,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    PostalCode = c.PostalCode,
                    Country = c.Country,
                    Type = c.Type.ToString(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    BranchId = c.User != null ? c.User.BranchId : null,
                    BranchName = c.User != null && c.User.Branch != null ? c.User.Branch.Name : null
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCustomerByUserId(int userId)
        {
            Customer? customer;
            if (IsCustomer())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || currentUserId.Value != userId)
                {
                    return Forbid();
                }

                customer = await _context.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }
            else
            {
                var query = await ApplyBranchScopeAsync(_context.Customers.AsNoTracking());
                customer = await query.FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found for this user" });
            }

            return Ok(customer);
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");
        private bool IsCustomer() => User.IsInRole("Customer");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<IQueryable<Customer>> ApplyBranchScopeAsync(IQueryable<Customer> query)
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

            var branchId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (!branchId.HasValue)
            {
                return query.Where(_ => false);
            }

            return query.Where(c =>
                (c.User != null && c.User.BranchId == branchId.Value) ||
                (c.CreatedByUser != null && c.CreatedByUser.BranchId == branchId.Value) ||
                c.Orders.Any(o => o.Agent != null && o.Agent.BranchId == branchId.Value) ||
                c.Tickets.Any(t => t.AssignedAgent != null && t.AssignedAgent.BranchId == branchId.Value));
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
    }
}
