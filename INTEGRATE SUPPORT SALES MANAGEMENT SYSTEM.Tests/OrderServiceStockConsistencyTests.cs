using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Tests;

public class OrderServiceStockConsistencyTests
{
    [Fact]
    public async Task CreateOrder_DeductsStock_AndRecalculatesTotals()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 10, price: 100m, sku: "SKU-100");

        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            OrderDetails =
            [
                new CreateOrderDetailDto
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    DiscountPercentage = 0
                }
            ],
            TaxAmount = 10m,
            DiscountAmount = 5m,
            FinalAmount = 0m
        };

        var created = await fixture.Service.CreateOrderAsync(dto);

        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var order = await fixture.Context.Orders.SingleAsync(o => o.Id == created.Id);

        Assert.Equal(8, updatedProduct.StockQuantity);
        Assert.Equal(200m, order.TotalAmount);
        Assert.Equal(205m, order.FinalAmount);

        fixture.InventoryMock.Verify(
            m => m.UpdateStockAsync("SKU-100", It.Is<UpdateInventoryStockDto>(u => u.Quantity == 8), It.IsAny<int?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_PendingToCancelled_RestocksStock()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 10, price: 50m, sku: "SKU-200");
        var order = await CreateOrderAsync(fixture.Service, customer.Id, product.Id, qty: 3);

        var cancelled = await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled);
        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var updatedOrder = await fixture.Context.Orders.SingleAsync(o => o.Id == order.Id);

        Assert.True(cancelled);
        Assert.Equal(10, updatedProduct.StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_CancelledToProcessing_DeductsStockAgain()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 10, price: 40m, sku: "SKU-300");
        var order = await CreateOrderAsync(fixture.Service, customer.Id, product.Id, qty: 4);

        await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled);
        await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var updatedOrder = await fixture.Context.Orders.SingleAsync(o => o.Id == order.Id);

        Assert.Equal(6, updatedProduct.StockQuantity);
        Assert.Equal(OrderStatus.Processing, updatedOrder.Status);
    }

    [Fact]
    public async Task DeleteOrder_CancelsAndRestocks()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 5, price: 20m, sku: "SKU-400");
        var order = await CreateOrderAsync(fixture.Service, customer.Id, product.Id, qty: 2);

        var deleted = await fixture.Service.DeleteOrderAsync(order.Id);
        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var updatedOrder = await fixture.Context.Orders.SingleAsync(o => o.Id == order.Id);

        Assert.True(deleted);
        Assert.Equal(5, updatedProduct.StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_ProcessingToRefunded_RestocksStock()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 12, price: 30m, sku: "SKU-500");
        var order = await CreateOrderAsync(fixture.Service, customer.Id, product.Id, qty: 5);

        await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);
        var refunded = await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Refunded);

        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var updatedOrder = await fixture.Context.Orders.SingleAsync(o => o.Id == order.Id);

        Assert.True(refunded);
        Assert.Equal(12, updatedProduct.StockQuantity);
        Assert.Equal(OrderStatus.Refunded, updatedOrder.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_RefundedToShipped_DeductsStockAgain()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 9, price: 25m, sku: "SKU-600");
        var order = await CreateOrderAsync(fixture.Service, customer.Id, product.Id, qty: 3);

        await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Refunded);
        await fixture.Service.UpdateOrderStatusAsync(order.Id, OrderStatus.Shipped);

        var updatedProduct = await fixture.Context.Products.SingleAsync(p => p.Id == product.Id);
        var updatedOrder = await fixture.Context.Orders.SingleAsync(o => o.Id == order.Id);

        Assert.Equal(6, updatedProduct.StockQuantity);
        Assert.Equal(OrderStatus.Shipped, updatedOrder.Status);
    }

    [Fact]
    public async Task CreateOrder_WhenStockInsufficient_ThrowsAndDoesNotPartiallyWrite()
    {
        using var fixture = CreateFixture();
        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 2, price: 99m, sku: "SKU-700");

        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            OrderDetails =
            [
                new CreateOrderDetailDto
                {
                    ProductId = product.Id,
                    Quantity = 5,
                    DiscountPercentage = 0
                }
            ],
            TaxAmount = 0m,
            DiscountAmount = 0m
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateOrderAsync(dto));
        Assert.Contains("Insufficient stock", ex.Message);

        await using var verifyContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(fixture.Connection)
                .Options);

        var productAfter = await verifyContext.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        var orderCount = await verifyContext.Orders.AsNoTracking().CountAsync();
        var detailCount = await verifyContext.OrderDetails.AsNoTracking().CountAsync();

        Assert.Equal(2, productAfter.StockQuantity);
        Assert.Equal(0, orderCount);
        Assert.Equal(0, detailCount);
    }

    [Fact]
    public async Task CreateOrder_WhenErpSyncFails_RollsBackOrderAndStock()
    {
        using var fixture = CreateFixture();
        fixture.InventoryMock
            .Setup(m => m.UpdateStockAsync(It.IsAny<string>(), It.IsAny<UpdateInventoryStockDto>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        var customer = await SeedCustomerAsync(fixture.Context);
        var product = await SeedProductAsync(fixture.Context, stock: 6, price: 80m, sku: "SKU-ERP-FAIL");

        var dto = new CreateOrderDto
        {
            CustomerId = customer.Id,
            OrderDetails =
            [
                new CreateOrderDetailDto
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    DiscountPercentage = 0
                }
            ],
            TaxAmount = 0m,
            DiscountAmount = 0m
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateOrderAsync(dto));
        Assert.Contains("ERP stock sync failed", ex.Message);

        await using var verifyContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(fixture.Connection)
                .Options);

        var productAfter = await verifyContext.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        var orderCount = await verifyContext.Orders.AsNoTracking().CountAsync();
        var detailCount = await verifyContext.OrderDetails.AsNoTracking().CountAsync();

        Assert.Equal(6, productAfter.StockQuantity);
        Assert.Equal(0, orderCount);
        Assert.Equal(0, detailCount);
    }

    private static async Task<Order> CreateOrderAsync(OrderService service, int customerId, int productId, int qty)
    {
        var created = await service.CreateOrderAsync(new CreateOrderDto
        {
            CustomerId = customerId,
            OrderDetails =
            [
                new CreateOrderDetailDto
                {
                    ProductId = productId,
                    Quantity = qty,
                    DiscountPercentage = 0
                }
            ],
            TaxAmount = 0m,
            DiscountAmount = 0m
        });

        return new Order { Id = created.Id };
    }

    private static async Task<Customer> SeedCustomerAsync(ApplicationDbContext context)
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "Customer",
            Email = $"{Guid.NewGuid():N}@example.com",
            Phone = "09123456789",
            CreatedAt = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Product> SeedProductAsync(ApplicationDbContext context, int stock, decimal price, string sku)
    {
        var product = new Product
        {
            Name = $"Product-{sku}",
            SKU = sku,
            Price = price,
            StockQuantity = stock,
            IsActive = true,
            Category = ProductCategory.TShirt,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static TestFixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        var inventoryMock = new Mock<IErpInventoryService>();
        inventoryMock
            .Setup(m => m.UpdateStockAsync(It.IsAny<string>(), It.IsAny<UpdateInventoryStockDto>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var emailMock = new Mock<IEmailService>();
        emailMock.Setup(m => m.SendPurchaseConfirmationEmailAsync(It.IsAny<PurchaseConfirmationData>())).ReturnsAsync(true);
        emailMock.Setup(m => m.SendOrderStatusUpdateEmailAsync(It.IsAny<OrderStatusUpdateData>())).ReturnsAsync(true);
        emailMock.Setup(m => m.SendShipmentTrackingEmailAsync(It.IsAny<ShipmentTrackingData>())).ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<OrderService>>();

        var service = new OrderService(context, loggerMock.Object, inventoryMock.Object, emailMock.Object);
        return new TestFixture(connection, context, service, inventoryMock);
    }

    private sealed class TestFixture : IDisposable
    {
        public TestFixture(
            SqliteConnection connection,
            ApplicationDbContext context,
            OrderService service,
            Mock<IErpInventoryService> inventoryMock)
        {
            Connection = connection;
            Context = context;
            Service = service;
            InventoryMock = inventoryMock;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public OrderService Service { get; }
        public Mock<IErpInventoryService> InventoryMock { get; }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }
}
