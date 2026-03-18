using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
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
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(customer => new CustomerDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Company = customer.Company,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    PostalCode = customer.PostalCode,
                    Country = customer.Country,
                    Type = customer.Type.ToString(),
                    CreatedAt = customer.CreatedAt,
                    UpdatedAt = customer.UpdatedAt,
                    TotalCalls = customer.Calls.Count(),
                    OpenTickets = customer.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = customer.Orders.Count(),
                    BranchId = customer.User != null ? customer.User.BranchId : null,
                    BranchName = customer.User != null && customer.User.Branch != null ? customer.User.Branch.Name : null,
                    TotalSpent = customer.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createCustomerDto)
        {
            var resolvedBranchId = await ResolveBranchIdAsync(createCustomerDto);
            var resolvedCompany = await ResolveCompanyNameAsync(createCustomerDto.Company);

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == createCustomerDto.Email.ToLower());

            if (existingCustomer != null)
            {
                throw new InvalidOperationException("Customer with this email already exists");
            }

            var customer = new Customer
            {
                FirstName = createCustomerDto.FirstName,
                LastName = createCustomerDto.LastName,
                Email = createCustomerDto.Email,
                Phone = createCustomerDto.Phone,
                Company = resolvedCompany,
                Address = createCustomerDto.Address,
                City = createCustomerDto.City,
                State = createCustomerDto.State,
                PostalCode = createCustomerDto.PostalCode,
                Country = createCustomerDto.Country,
                Type = CustomerType.Individual,
                UserId = createCustomerDto.UserId,
                CreatedByUserId = createCustomerDto.CreatedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);

            if (resolvedBranchId.HasValue && customer.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(customer.UserId.Value);
                if (user != null && !user.BranchId.HasValue)
                {
                    user.BranchId = resolvedBranchId.Value;
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return new CustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Company = customer.Company,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                Type = customer.Type.ToString(),
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                TotalCalls = 0,
                OpenTickets = 0,
                TotalOrders = 0,
                BranchId = resolvedBranchId,
                BranchName = resolvedBranchId.HasValue
                    ? await _context.Branches
                        .Where(b => b.Id == resolvedBranchId.Value)
                        .Select(b => b.Name)
                        .FirstOrDefaultAsync()
                    : null,
                TotalSpent = 0
            };
        }

        public async Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerDto updateCustomerDto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            if (!string.IsNullOrEmpty(updateCustomerDto.FirstName))
                customer.FirstName = updateCustomerDto.FirstName;

            if (!string.IsNullOrEmpty(updateCustomerDto.LastName))
                customer.LastName = updateCustomerDto.LastName;

            if (!string.IsNullOrEmpty(updateCustomerDto.Email))
            {
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == updateCustomerDto.Email.ToLower() && c.Id != id);

                if (existingCustomer != null)
                {
                    throw new InvalidOperationException("Email is already taken by another customer");
                }

                customer.Email = updateCustomerDto.Email;
            }

            if (updateCustomerDto.Phone != null)
                customer.Phone = updateCustomerDto.Phone;

            if (updateCustomerDto.Company != null)
                customer.Company = updateCustomerDto.Company;

            if (updateCustomerDto.Address != null)
                customer.Address = updateCustomerDto.Address;

            if (updateCustomerDto.City != null)
                customer.City = updateCustomerDto.City;

            if (updateCustomerDto.State != null)
                customer.State = updateCustomerDto.State;

            if (updateCustomerDto.PostalCode != null)
                customer.PostalCode = updateCustomerDto.PostalCode;

            if (updateCustomerDto.Country != null)
                customer.Country = updateCustomerDto.Country;

            customer.Type = CustomerType.Individual;

            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetCustomerByIdAsync(id);
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.Tickets)
                .Include(c => c.Calls)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return false;

            if (customer.Orders.Any() || customer.Tickets.Any() || customer.Calls.Any())
                return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
        {
            var normalizedEmail = email.ToLower();
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.Email.ToLower() == normalizedEmail)
                .Select(customer => new CustomerDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Company = customer.Company,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    PostalCode = customer.PostalCode,
                    Country = customer.Country,
                    Type = customer.Type.ToString(),
                    CreatedAt = customer.CreatedAt,
                    UpdatedAt = customer.UpdatedAt,
                    TotalCalls = customer.Calls.Count(),
                    OpenTickets = customer.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = customer.Orders.Count(),
                    BranchId = customer.User != null ? customer.User.BranchId : null,
                    BranchName = customer.User != null && customer.User.Branch != null ? customer.User.Branch.Name : null,
                    TotalSpent = customer.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CustomerInteractionDto> GetCustomerInteractionHistoryAsync(int customerId)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Id, c.FirstName, c.LastName })
                .FirstOrDefaultAsync();

            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            var recentCalls = await _context.Calls
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId)
                .OrderByDescending(c => c.StartTime)
                .Take(5)
                .Select(c => new CallDto
                {
                    Id = c.Id,
                    AgentId = c.AgentId,
                    Type = c.Type,
                    Status = c.Status,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    Duration = c.Duration,
                    Subject = c.Subject,
                    Notes = c.Notes,
                    Outcome = c.Outcome,
                    IsEscalated = c.IsEscalated,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var recentTickets = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    FinalAmount = o.FinalAmount,
                    OrderDate = o.OrderDate,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return new CustomerInteractionDto
            {
                CustomerId = customer.Id,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                RecentCalls = recentCalls,
                RecentTickets = recentTickets,
                RecentOrders = recentOrders
            };
        }

        public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm)
        {
            var normalizedSearchTerm = searchTerm.ToLower();
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.FirstName.ToLower().Contains(normalizedSearchTerm) ||
                           c.LastName.ToLower().Contains(normalizedSearchTerm) ||
                           c.Email.ToLower().Contains(normalizedSearchTerm) ||
                           (c.Company != null && c.Company.ToLower().Contains(normalizedSearchTerm)))
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
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
        }

        public async Task<CustomerDto?> GetCustomerByUserIdAsync(int userId)
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(customer => new CustomerDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Company = customer.Company,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    PostalCode = customer.PostalCode,
                    Country = customer.Country,
                    Type = customer.Type.ToString(),
                    CreatedAt = customer.CreatedAt,
                    UpdatedAt = customer.UpdatedAt,
                    TotalCalls = customer.Calls.Count(),
                    OpenTickets = customer.Tickets.Count(t => t.Status != TicketStatus.Closed),
                    TotalOrders = customer.Orders.Count(),
                    BranchId = customer.User != null ? customer.User.BranchId : null,
                    BranchName = customer.User != null && customer.User.Branch != null ? customer.User.Branch.Name : null,
                    TotalSpent = customer.Orders
                        .Where(o => o.Status != OrderStatus.Cancelled)
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0m
                })
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ResolveBranchIdAsync(CreateCustomerDto createCustomerDto)
        {
            if (createCustomerDto.BranchId.HasValue)
            {
                return createCustomerDto.BranchId.Value;
            }

            if (createCustomerDto.UserId.HasValue)
            {
                var linkedUserBranchId = await _context.Users
                    .Where(u => u.Id == createCustomerDto.UserId.Value)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();

                if (linkedUserBranchId.HasValue)
                {
                    return linkedUserBranchId.Value;
                }
            }

            if (createCustomerDto.CreatedByUserId.HasValue)
            {
                var creatorBranchId = await _context.Users
                    .Where(u => u.Id == createCustomerDto.CreatedByUserId.Value)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();

                if (creatorBranchId.HasValue)
                {
                    return creatorBranchId.Value;
                }
            }

            return await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (int?)b.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> ResolveCompanyNameAsync(string? requestedCompany)
        {
            if (!string.IsNullOrWhiteSpace(requestedCompany))
            {
                return requestedCompany.Trim();
            }

            return await _context.TenantSubscriptions
                .Where(t => !string.IsNullOrWhiteSpace(t.TenantName))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .Select(t => t.TenantName)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> LinkCustomerToUserAsync(int customerId, int userId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null) return false;
            customer.UserId = userId;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
