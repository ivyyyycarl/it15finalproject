using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Stripe;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IDataChangeNotifier _notifier;
        private readonly ApplicationDbContext _context;
        private readonly IStripeService _stripeService;
        
        public OrdersController(IOrderService orderService, IDataChangeNotifier notifier, ApplicationDbContext context, IStripeService stripeService)
        {
            _orderService = orderService;
            _notifier = notifier;
            _context = context;
            _stripeService = stripeService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var query = _context.Orders.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName) : string.Empty,
                    AgentId = o.AgentId,
                    AgentName = o.Agent != null ? (o.Agent.FirstName + " " + o.Agent.LastName) : string.Empty,
                    RelatedCallId = o.RelatedCallId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    TaxAmount = o.TaxAmount,
                    DiscountAmount = o.DiscountAmount,
                    FinalAmount = o.FinalAmount,
                    OrderDate = o.OrderDate,
                    ShippingDate = o.ShippingDate,
                    DeliveryDate = o.DeliveryDate,
                    ShippingAddress = o.ShippingAddress,
                    BillingAddress = o.BillingAddress,
                    Notes = o.Notes,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    ItemCount = o.OrderDetails.Count(),
                    PaymentStatus = o.PaymentStatus,
                    PaymentIntentId = o.PaymentIntentId
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetOrdersPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? customerId = null,
            [FromQuery] int? agentId = null,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] PaymentStatus? paymentStatus = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortDir = "desc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _context.Orders.AsNoTracking().AsQueryable();
            query = await ApplyBranchScopeAsync(query);

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            if (agentId.HasValue)
            {
                query = query.Where(o => o.AgentId == agentId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(o => o.OrderDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                var inclusiveDateTo = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= inclusiveDateTo);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(term) ||
                    (o.Customer != null && (
                        o.Customer.FirstName.ToLower().Contains(term) ||
                        o.Customer.LastName.ToLower().Contains(term) ||
                        (o.Customer.FirstName + " " + o.Customer.LastName).ToLower().Contains(term))));
            }

            var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy ?? "createdAt").ToLowerInvariant() switch
            {
                "orderdate" => isDesc ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
                "ordernumber" => isDesc ? query.OrderByDescending(o => o.OrderNumber) : query.OrderBy(o => o.OrderNumber),
                "totalamount" => isDesc ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => isDesc ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                "paymentstatus" => isDesc ? query.OrderByDescending(o => o.PaymentStatus) : query.OrderBy(o => o.PaymentStatus),
                "customername" => isDesc
                    ? query.OrderByDescending(o => o.Customer != null ? o.Customer.LastName : string.Empty).ThenByDescending(o => o.Customer != null ? o.Customer.FirstName : string.Empty)
                    : query.OrderBy(o => o.Customer != null ? o.Customer.LastName : string.Empty).ThenBy(o => o.Customer != null ? o.Customer.FirstName : string.Empty),
                _ => isDesc ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName) : string.Empty,
                    AgentId = o.AgentId,
                    AgentName = o.Agent != null ? (o.Agent.FirstName + " " + o.Agent.LastName) : string.Empty,
                    RelatedCallId = o.RelatedCallId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    TaxAmount = o.TaxAmount,
                    DiscountAmount = o.DiscountAmount,
                    FinalAmount = o.FinalAmount,
                    OrderDate = o.OrderDate,
                    ShippingDate = o.ShippingDate,
                    DeliveryDate = o.DeliveryDate,
                    ShippingAddress = o.ShippingAddress,
                    BillingAddress = o.BillingAddress,
                    Notes = o.Notes,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    ItemCount = o.OrderDetails.Count(),
                    PaymentStatus = o.PaymentStatus,
                    PaymentIntentId = o.PaymentIntentId
                })
                .ToListAsync();

            return Ok(PagedResultDto<OrderDto>.Create(items, page, pageSize, totalCount));
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var scopedQuery = await ApplyBranchScopeAsync(_context.Orders.AsNoTracking());
            var allowed = await scopedQuery.AnyAsync(o => o.Id == id);
            if (!allowed)
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }
        
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetOrdersByAgent(int agentId)
        {
            var query = _context.Orders.AsNoTracking().Where(o => o.AgentId == agentId);
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName) : string.Empty,
                    AgentId = o.AgentId,
                    AgentName = o.Agent != null ? (o.Agent.FirstName + " " + o.Agent.LastName) : string.Empty,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    TaxAmount = o.TaxAmount,
                    DiscountAmount = o.DiscountAmount,
                    FinalAmount = o.FinalAmount,
                    OrderDate = o.OrderDate,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    ItemCount = o.OrderDetails.Count(),
                    PaymentStatus = o.PaymentStatus,
                    PaymentIntentId = o.PaymentIntentId
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomer(int customerId)
        {
            var query = _context.Orders.AsNoTracking().Where(o => o.CustomerId == customerId);
            query = await ApplyBranchScopeAsync(query);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName) : string.Empty,
                    AgentId = o.AgentId,
                    AgentName = o.Agent != null ? (o.Agent.FirstName + " " + o.Agent.LastName) : string.Empty,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    TaxAmount = o.TaxAmount,
                    DiscountAmount = o.DiscountAmount,
                    FinalAmount = o.FinalAmount,
                    OrderDate = o.OrderDate,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    ItemCount = o.OrderDetails.Count(),
                    PaymentStatus = o.PaymentStatus,
                    PaymentIntentId = o.PaymentIntentId
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpPost]
        [Authorize(Roles = "Customer,Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            if (!IsSuperAdmin())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                if (IsCustomer())
                {
                    var customer = await ResolveCustomerForCheckoutAsync(currentUserId.Value, createOrderDto.CustomerId);

                    if (customer == null)
                    {
                        return Forbid();
                    }

                    // Always bind checkout to the authenticated customer's profile.
                    createOrderDto.CustomerId = customer.Id;

                    if (createOrderDto.OrderDetails == null || createOrderDto.OrderDetails.Count == 0)
                    {
                        return BadRequest(new { message = "Order details are required." });
                    }

                    var productIds = createOrderDto.OrderDetails
                        .Select(d => d.ProductId)
                        .Distinct()
                        .ToList();
                    var branchIds = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .Select(p => p.BranchId)
                        .ToListAsync();

                    var distinctBranchIds = branchIds
                        .Where(b => b.HasValue)
                        .Select(b => b!.Value)
                        .Distinct()
                        .ToList();

                    if (distinctBranchIds.Count > 1)
                    {
                        return BadRequest(new { message = "Please checkout items from one branch at a time." });
                    }

                    var checkoutBranchId = distinctBranchIds.FirstOrDefault();
                    if (checkoutBranchId > 0 && customer.User != null)
                    {
                        if (!customer.User.BranchId.HasValue)
                        {
                            // Auto-assign customer branch from the branch being shopped.
                            customer.User.BranchId = checkoutBranchId;
                            customer.User.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Customer self-checkout should not auto-assign the customer as an agent.
                    createOrderDto.AgentId = null;
                }
                else
                {
                    if (!createOrderDto.AgentId.HasValue)
                    {
                        createOrderDto.AgentId = currentUserId.Value;
                    }

                    var currentBranchId = await GetCurrentBranchIdAsync();
                    if (!currentBranchId.HasValue)
                    {
                        return Forbid();
                    }

                    var customerBranchId = await _context.Customers
                        .Where(c => c.Id == createOrderDto.CustomerId)
                        .Select(c => c.User != null ? c.User.BranchId : null)
                        .FirstOrDefaultAsync();

                    if (!customerBranchId.HasValue || customerBranchId.Value != currentBranchId.Value)
                    {
                        return Forbid();
                    }

                    if (createOrderDto.AgentId.HasValue)
                    {
                        var agentBranchId = await _context.Users
                            .Where(u => u.Id == createOrderDto.AgentId.Value && u.IsActive)
                            .Select(u => u.BranchId)
                            .FirstOrDefaultAsync();

                        if (!agentBranchId.HasValue || agentBranchId.Value != currentBranchId.Value)
                        {
                            return Forbid();
                        }
                    }
                }
            }

            var order = await _orderService.CreateOrderAsync(createOrderDto);
            await _notifier.NotifyDataChanged("Order", "Created");
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            if (!await CanAccessOrderAsync(id))
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = await _orderService.UpdateOrderAsync(id, updateOrderDto);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            await _notifier.NotifyDataChanged("Order", "Updated");
            return Ok(order);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            if (!await CanAccessOrderAsync(id))
            {
                return NotFound(new { message = "Order not found" });
            }

            var result = await _orderService.DeleteOrderAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Order not found" });
            }

            await _notifier.NotifyDataChanged("Order", "Deleted");
            return NoContent();
        }
        
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateStatusDto)
        {
            if (!await CanAccessOrderAsync(id))
            {
                return NotFound(new { message = "Order not found" });
            }

            var result = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto.Status);
            if (!result)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(new { message = "Order status updated successfully" });
        }

        [HttpPost("{id}/refund-request")]
        [Authorize(Roles = "Agent,Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> RequestRefund(int id, [FromBody] RefundActionDto request)
        {
            if (!await CanAccessOrderAsync(id))
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
            {
                return BadRequest(new { message = "Refund cannot be requested for this order status." });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User context is invalid." });
            }

            var hasPendingRequest = await _context.RefundRequests
                .AnyAsync(r => r.OrderId == id && r.Status == RefundRequestStatus.Pending);
            if (hasPendingRequest)
            {
                return BadRequest(new { message = "A pending refund request already exists for this order." });
            }

            _context.RefundRequests.Add(new RefundRequest
            {
                OrderId = id,
                RequestedByUserId = userId.Value,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                Status = RefundRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            var userEmail = await GetCurrentUserEmailAsync();
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided." : request.Reason.Trim();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "RefundRequest",
                Description = $"Refund requested for order {order.OrderNumber}",
                UserId = userId,
                UserEmail = userEmail,
                Details = $"OrderId={order.Id};Reason={reason}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Refund request submitted. Admin approval is required." });
        }

        [HttpPost("{id}/refund/approve")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ApproveRefund(int id, [FromBody] RefundActionDto request)
        {
            if (!await CanAccessOrderAsync(id))
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            if (order.Status == OrderStatus.Refunded)
            {
                return BadRequest(new { message = "Order is already refunded." });
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new { message = "Cancelled orders cannot be refunded." });
            }

            var pendingRequest = await _context.RefundRequests
                .Where(r => r.OrderId == id && r.Status == RefundRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
            if (pendingRequest == null)
            {
                return BadRequest(new { message = "No pending refund request found for this order." });
            }

            string? refundReference = null;
            if (!string.IsNullOrWhiteSpace(order.PaymentIntentId))
            {
                try
                {
                    var refund = await _stripeService.CreateRefundAsync(order.PaymentIntentId, request.Amount, request.Reason);
                    refundReference = refund.Id;
                }
                catch (StripeException ex)
                {
                    return StatusCode(502, new
                    {
                        message = "Stripe refund failed.",
                        detail = ex.StripeError?.Message ?? ex.Message
                    });
                }
            }

            var statusUpdated = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Refunded);
            if (!statusUpdated)
            {
                return BadRequest(new { message = "Unable to update order to refunded status." });
            }

            order.PaymentStatus = PaymentStatus.Refunded;
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided." : request.Reason.Trim();
            var refundMeta = !string.IsNullOrWhiteSpace(refundReference)
                ? $"RefundRef={refundReference};Reason={reason}"
                : $"ManualRefund;Reason={reason}";
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"Refund approved. {refundMeta}"
                : $"{order.Notes}\nRefund approved. {refundMeta}";
            order.UpdatedAt = DateTime.UtcNow;

            var userId = GetCurrentUserId();
            var userEmail = await GetCurrentUserEmailAsync();
            if (userId.HasValue)
            {
                pendingRequest.Status = RefundRequestStatus.Approved;
                pendingRequest.ApprovedByUserId = userId.Value;
                pendingRequest.ApprovedAt = DateTime.UtcNow;
                pendingRequest.UpdatedAt = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "RefundApproved",
                Description = $"Refund approved for order {order.OrderNumber}",
                UserId = userId,
                UserEmail = userEmail,
                Details = $"OrderId={order.Id};{refundMeta}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _notifier.NotifyDataChanged("Order", "Refunded");

            return Ok(new
            {
                message = "Refund approved and order marked as refunded.",
                refundReference
            });
        }

        [HttpGet("refund-requests/pending")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetPendingRefundRequests()
        {
            var scopedOrders = await ApplyBranchScopeAsync(_context.Orders.AsNoTracking());
            var scopedOrderIds = await scopedOrders.Select(o => o.Id).ToListAsync();

            var items = await _context.RefundRequests
                .AsNoTracking()
                .Where(r => r.Status == RefundRequestStatus.Pending && scopedOrderIds.Contains(r.OrderId))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new PendingRefundRequestDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    OrderNumber = r.Order.OrderNumber,
                    Status = r.Status,
                    Reason = r.Reason,
                    RequestedByUserId = r.RequestedByUserId,
                    RequestedByName = r.RequestedByUser.FirstName + " " + r.RequestedByUser.LastName,
                    CreatedAt = r.CreatedAt,
                    TotalAmount = r.Order.TotalAmount
                })
                .ToListAsync();

            return Ok(items);
        }
        
        [HttpPost("calculate-total")]
        public async Task<IActionResult> CalculateOrderTotal([FromBody] List<CreateOrderDetailDto> orderDetails)
        {
            var total = await _orderService.CalculateOrderTotalAsync(orderDetails);
            return Ok(new { total });
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");
        private bool IsCustomer() => User.IsInRole("Customer");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<IQueryable<Order>> ApplyBranchScopeAsync(IQueryable<Order> query)
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

            var scoped = query.Where(o =>
                (o.Agent != null && o.Agent.BranchId == branchId.Value) ||
                (o.Customer != null && o.Customer.User != null && o.Customer.User.BranchId == branchId.Value));

            return scoped;
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

        private async Task<bool> CanAccessOrderAsync(int orderId)
        {
            var scopedQuery = await ApplyBranchScopeAsync(_context.Orders.AsNoTracking());
            return await scopedQuery.AnyAsync(o => o.Id == orderId);
        }

        private async Task<string> GetCurrentUserEmailAsync()
        {
            var claimEmail = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(claimEmail))
            {
                return claimEmail;
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return "unknown";
            }

            var email = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(email) ? "unknown" : email;
        }

        private async Task<Models.Customer?> ResolveCustomerForCheckoutAsync(int currentUserId, int requestedCustomerId)
        {
            var currentUser = await _context.Users
                .Where(u => u.Id == currentUserId && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Phone,
                    u.BranchId
                })
                .FirstOrDefaultAsync();

            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Email))
            {
                return null;
            }

            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (customer != null)
            {
                return customer;
            }

            if (requestedCustomerId > 0)
            {
                var requestedCustomer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == requestedCustomerId);

                if (requestedCustomer != null)
                {
                    if (requestedCustomer.UserId == currentUserId)
                    {
                        return requestedCustomer;
                    }

                    if (!requestedCustomer.UserId.HasValue
                        && string.Equals(requestedCustomer.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        requestedCustomer.UserId = currentUserId;
                        requestedCustomer.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return requestedCustomer;
                    }
                }
            }

            var emailMatchedCustomer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Email == currentUser.Email);

            if (emailMatchedCustomer != null)
            {
                if (!emailMatchedCustomer.UserId.HasValue)
                {
                    emailMatchedCustomer.UserId = currentUserId;
                    emailMatchedCustomer.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return emailMatchedCustomer.UserId == currentUserId ? emailMatchedCustomer : null;
            }

            var resolvedCompany = await _context.TenantSubscriptions
                .Where(t => !string.IsNullOrWhiteSpace(t.TenantName))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .Select(t => t.TenantName)
                .FirstOrDefaultAsync();

            var customerUser = await _context.Users.FindAsync(currentUserId);
            if (customerUser != null && !customerUser.BranchId.HasValue)
            {
                customerUser.BranchId = await _context.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Id)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefaultAsync();
                customerUser.UpdatedAt = DateTime.UtcNow;
            }

            var createdCustomer = new Models.Customer
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                Email = currentUser.Email,
                Phone = currentUser.Phone ?? string.Empty,
                Company = string.IsNullOrWhiteSpace(resolvedCompany) ? null : resolvedCompany.Trim(),
                UserId = currentUserId,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(createdCustomer);
            await _context.SaveChangesAsync();

            return createdCustomer;
        }
    }
    
    public class UpdateOrderStatusDto
    {
        public Models.OrderStatus Status { get; set; }
    }

    public class RefundActionDto
    {
        public string? Reason { get; set; }
        public decimal? Amount { get; set; }
    }

    public class PendingRefundRequestDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int RequestedByUserId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
