using System;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IErpInventoryService _inventoryService;
        private readonly IEmailService _emailService;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger, IErpInventoryService inventoryService, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _inventoryService = inventoryService;
            _emailService = emailService;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .AsNoTracking()
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
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(order => new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer != null ? (order.Customer.FirstName + " " + order.Customer.LastName) : string.Empty,
                    AgentId = order.AgentId,
                    AgentName = order.Agent != null ? (order.Agent.FirstName + " " + order.Agent.LastName) : string.Empty,
                    RelatedCallId = order.RelatedCallId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    TaxAmount = order.TaxAmount,
                    DiscountAmount = order.DiscountAmount,
                    FinalAmount = order.FinalAmount,
                    OrderDate = order.OrderDate,
                    ShippingDate = order.ShippingDate,
                    DeliveryDate = order.DeliveryDate,
                    ShippingAddress = order.ShippingAddress,
                    BillingAddress = order.BillingAddress,
                    Notes = order.Notes,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt,
                    ItemCount = order.OrderDetails.Count(),
                    PaymentStatus = order.PaymentStatus,
                    PaymentIntentId = order.PaymentIntentId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (createOrderDto.OrderDetails == null || createOrderDto.OrderDetails.Count == 0)
                        throw new InvalidOperationException("Order must contain at least one item.");

                    var orderNumber = await GenerateOrderNumberAsync();

                    var order = new Order
                    {
                        OrderNumber = orderNumber,
                        CustomerId = createOrderDto.CustomerId,
                        AgentId = createOrderDto.AgentId,
                        RelatedCallId = createOrderDto.RelatedCallId,
                        Status = OrderStatus.Pending,
                        TotalAmount = 0,
                        TaxAmount = createOrderDto.TaxAmount,
                        DiscountAmount = createOrderDto.DiscountAmount,
                        FinalAmount = 0,
                        OrderDate = DateTime.UtcNow,
                        ShippingAddress = createOrderDto.ShippingAddress,
                        BillingAddress = createOrderDto.BillingAddress,
                        Notes = createOrderDto.Notes,
                        CreatedAt = DateTime.UtcNow,
                        PaymentIntentId = createOrderDto.PaymentIntentId,
                        PaymentStatus = createOrderDto.PaymentStatus
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    var requiredByProduct = createOrderDto.OrderDetails
                        .GroupBy(d => d.ProductId)
                        .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                        .ToList();

                    var products = await _context.Products
                        .Where(p => requiredByProduct.Select(r => r.ProductId).Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id);

                    foreach (var requirement in requiredByProduct)
                    {
                        if (!products.TryGetValue(requirement.ProductId, out var product))
                            throw new InvalidOperationException($"Product not found: {requirement.ProductId}");

                        if (product.StockQuantity < requirement.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {requirement.Quantity}");
                        }
                    }

                    foreach (var detail in createOrderDto.OrderDetails)
                    {
                        var product = products[detail.ProductId];
                        var unitPrice = product.Price;
                        var lineTotal = Math.Round(unitPrice * detail.Quantity * (1 - (detail.DiscountPercentage / 100m)), 2, MidpointRounding.AwayFromZero);

                        _context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            UnitPrice = unitPrice,
                            DiscountPercentage = detail.DiscountPercentage,
                            TotalPrice = lineTotal,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();

                    await _context.Entry(order).Collection(o => o.OrderDetails).LoadAsync();
                    RecalculateOrderAmounts(order);
                    await ApplyStockDeltaForOrderAsync(order, -1, "OrderCreated");
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await SendPurchaseConfirmationAsync(order);

                    return (await GetOrderByIdAsync(order.Id))!;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderDto updateOrderDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (order == null) return null;

                    var oldStatus = order.Status;
                    var newStatus = updateOrderDto.Status ?? oldStatus;

                    if (updateOrderDto.CustomerId.HasValue)
                        order.CustomerId = updateOrderDto.CustomerId.Value;

                    if (updateOrderDto.AgentId.HasValue)
                        order.AgentId = updateOrderDto.AgentId.Value;

                    if (updateOrderDto.RelatedCallId.HasValue)
                        order.RelatedCallId = updateOrderDto.RelatedCallId.Value;

                    order.Status = newStatus;

                    if (updateOrderDto.TaxAmount.HasValue)
                        order.TaxAmount = updateOrderDto.TaxAmount.Value;

                    if (updateOrderDto.DiscountAmount.HasValue)
                        order.DiscountAmount = updateOrderDto.DiscountAmount.Value;

                    if (updateOrderDto.ShippingDate.HasValue)
                        order.ShippingDate = updateOrderDto.ShippingDate.Value;

                    if (updateOrderDto.DeliveryDate.HasValue)
                        order.DeliveryDate = updateOrderDto.DeliveryDate.Value;

                    if (updateOrderDto.ShippingAddress != null)
                        order.ShippingAddress = updateOrderDto.ShippingAddress;

                    if (updateOrderDto.BillingAddress != null)
                        order.BillingAddress = updateOrderDto.BillingAddress;

                    if (updateOrderDto.Notes != null)
                        order.Notes = updateOrderDto.Notes;

                    if (newStatus == OrderStatus.Shipped && !order.ShippingDate.HasValue)
                        order.ShippingDate = DateTime.UtcNow;

                    if (newStatus == OrderStatus.Delivered && !order.DeliveryDate.HasValue)
                        order.DeliveryDate = DateTime.UtcNow;

                    RecalculateOrderAmounts(order);
                    await ApplyStockTransitionDeltaAsync(order, oldStatus, newStatus);

                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return await GetOrderByIdAsync(id);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (order == null) return false;

                    var oldStatus = order.Status;
                    var newStatus = OrderStatus.Cancelled;

                    await ApplyStockTransitionDeltaAsync(order, oldStatus, newStatus);
                    order.Status = newStatus;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(int customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
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
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayCount = await _context.Orders.CountAsync(o => o.OrderNumber.StartsWith($"ORD-{datePrefix}"));
            return $"ORD-{datePrefix}-{(todayCount + 1):D4}";
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null) return false;

                    var oldStatus = order.Status;
                    if (oldStatus == status) return true;

                    await ApplyStockTransitionDeltaAsync(order, oldStatus, status);

                    order.Status = status;
                    order.UpdatedAt = DateTime.UtcNow;

                    if (status == OrderStatus.Shipped && !order.ShippingDate.HasValue)
                        order.ShippingDate = DateTime.UtcNow;

                    if (status == OrderStatus.Delivered && !order.DeliveryDate.HasValue)
                        order.DeliveryDate = DateTime.UtcNow;

                    RecalculateOrderAmounts(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await SendOrderStatusEmailAsync(order, oldStatus, status);

                    if (status == OrderStatus.Shipped)
                        await SendShipmentEmailAsync(order);

                    await LogEmailAuditAsync(order.OrderNumber, order.Customer?.Email ?? "unknown", $"Order status changed: {oldStatus} -> {status}");

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByAgentAsync(int agentId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.AgentId == agentId)
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
        }

        public async Task<decimal> CalculateOrderTotalAsync(List<CreateOrderDetailDto> orderDetails)
        {
            decimal total = 0;
            foreach (var detail in orderDetails)
            {
                total += detail.UnitPrice * detail.Quantity;
            }
            return await Task.FromResult(total);
        }

        private static bool IsStockCommittedStatus(OrderStatus status)
        {
            return status != OrderStatus.Cancelled && status != OrderStatus.Refunded;
        }

        private async Task ApplyStockTransitionDeltaAsync(Order order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            var oldCommitted = IsStockCommittedStatus(oldStatus);
            var newCommitted = IsStockCommittedStatus(newStatus);

            if (oldCommitted == newCommitted)
            {
                return;
            }

            var multiplier = newCommitted ? -1 : 1;
            var reason = newCommitted
                ? $"OrderStatusChanged:{oldStatus}->{newStatus}:Deduct"
                : $"OrderStatusChanged:{oldStatus}->{newStatus}:Restock";

            await ApplyStockDeltaForOrderAsync(order, multiplier, reason);
        }

        private async Task ApplyStockDeltaForOrderAsync(Order order, int multiplier, string reason)
        {
            if (order.OrderDetails == null || order.OrderDetails.Count == 0)
            {
                return;
            }

            var groupedDeltas = order.OrderDetails
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, QuantityDelta = g.Sum(x => x.Quantity) * multiplier })
                .Where(x => x.QuantityDelta != 0)
                .ToList();

            var productIds = groupedDeltas.Select(x => x.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var delta in groupedDeltas)
            {
                if (!products.TryGetValue(delta.ProductId, out var product))
                    throw new InvalidOperationException($"Product not found while applying stock delta. ProductId: {delta.ProductId}");

                var oldStock = product.StockQuantity;
                var newStock = oldStock + delta.QuantityDelta;
                if (newStock < 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {product.Name}. Available: {oldStock}, Requested change: {delta.QuantityDelta}");
                }

                if (!string.IsNullOrWhiteSpace(product.SKU))
                {
                    var erpUpdated = await _inventoryService.UpdateStockAsync(product.SKU, new UpdateInventoryStockDto
                    {
                        SKU = product.SKU,
                        Quantity = newStock,
                        Notes = reason
                    }, product.BranchId);

                    if (!erpUpdated)
                    {
                        throw new InvalidOperationException($"ERP stock sync failed for SKU {product.SKU}");
                    }
                }

                product.StockQuantity = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Stock adjusted ({Reason}) for product {ProductId}/{Sku}: {OldStock} -> {NewStock}",
                    reason,
                    product.Id,
                    product.SKU,
                    oldStock,
                    newStock);
            }
        }

        private static void RecalculateOrderAmounts(Order order)
        {
            if (order.OrderDetails == null || order.OrderDetails.Count == 0)
            {
                order.TotalAmount = 0;
                order.FinalAmount = Math.Round(Math.Max(0, order.TaxAmount - order.DiscountAmount), 2, MidpointRounding.AwayFromZero);
                return;
            }

            var subtotal = order.OrderDetails.Sum(d => d.TotalPrice);
            order.TotalAmount = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
            order.FinalAmount = Math.Round(Math.Max(0, order.TotalAmount + order.TaxAmount - order.DiscountAmount), 2, MidpointRounding.AwayFromZero);
        }

        private async Task SendPurchaseConfirmationAsync(Order order)
        {
            try
            {
                var fullOrder = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (fullOrder?.Customer == null) return;

                var erpSynced = fullOrder.OrderDetails.Any(od =>
                    od.Product != null && !string.IsNullOrEmpty(od.Product.SKU));

                var data = new PurchaseConfirmationData
                {
                    CustomerEmail = fullOrder.Customer.Email,
                    CustomerFirstName = fullOrder.Customer.FirstName,
                    CustomerLastName = fullOrder.Customer.LastName,
                    OrderNumber = fullOrder.OrderNumber,
                    OrderDate = fullOrder.OrderDate,
                    TransactionId = fullOrder.PaymentIntentId ?? $"TXN-{fullOrder.Id:D8}",
                    Subtotal = fullOrder.TotalAmount,
                    TaxAmount = fullOrder.TaxAmount,
                    DiscountAmount = fullOrder.DiscountAmount,
                    FinalAmount = fullOrder.FinalAmount,
                    PaymentMethod = fullOrder.PaymentStatus == PaymentStatus.Paid ? "Online Payment (Stripe)" : "Pending",
                    PaymentStatus = fullOrder.PaymentStatus.ToString(),
                    ShippingAddress = fullOrder.ShippingAddress,
                    EstimatedDeliveryDate = fullOrder.DeliveryDate ?? DateTime.UtcNow.AddDays(7),
                    ErpSynced = erpSynced,
                    Items = fullOrder.OrderDetails.Select(od => new PurchaseItemData
                    {
                        ProductName = od.Product?.Name ?? "Unknown Product",
                        SKU = od.Product?.SKU ?? "N/A",
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList()
                };

                await _emailService.SendPurchaseConfirmationEmailAsync(data);

                await LogEmailAuditAsync(fullOrder.OrderNumber, fullOrder.Customer.Email, "Purchase confirmation email sent");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send purchase confirmation email for order {OrderId}", order.Id);
            }
        }

        private async Task SendOrderStatusEmailAsync(Order order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            try
            {
                if (order.Customer == null)
                {
                    var fullOrder = await _context.Orders
                        .Include(o => o.Customer)
                        .FirstOrDefaultAsync(o => o.Id == order.Id);
                    if (fullOrder?.Customer == null) return;
                    order = fullOrder;
                }

                var data = new OrderStatusUpdateData
                {
                    CustomerEmail = order.Customer!.Email,
                    CustomerFirstName = order.Customer.FirstName,
                    OrderNumber = order.OrderNumber,
                    TransactionId = order.PaymentIntentId ?? $"TXN-{order.Id:D8}",
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    UpdatedAt = DateTime.UtcNow,
                    Notes = order.Notes
                };

                await _emailService.SendOrderStatusUpdateEmailAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order status email for order {OrderNumber}", order.OrderNumber);
            }
        }

        private async Task SendShipmentEmailAsync(Order order)
        {
            try
            {
                if (order.Customer == null) return;

                var data = new ShipmentTrackingData
                {
                    CustomerEmail = order.Customer.Email,
                    CustomerFirstName = order.Customer.FirstName,
                    OrderNumber = order.OrderNumber,
                    ShippingDate = order.ShippingDate ?? DateTime.UtcNow,
                    EstimatedDeliveryDate = order.DeliveryDate ?? DateTime.UtcNow.AddDays(5),
                    ShippingAddress = order.ShippingAddress
                };

                await _emailService.SendShipmentTrackingEmailAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send shipment email for order {OrderNumber}", order.OrderNumber);
            }
        }

        private async Task LogEmailAuditAsync(string orderNumber, string customerEmail, string description)
        {
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Email Notification",
                    Description = $"[Order {orderNumber}] {description}",
                    UserEmail = customerEmail,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Order: {orderNumber}, Recipient: {customerEmail}"
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log email audit for order {OrderNumber}", orderNumber);
            }
        }
    }
}
