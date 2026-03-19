using System.Security.Claims;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Tests;

public class BranchScopeControllerTests
{
    [Fact]
    public async Task ProductsController_GetAllProducts_ReturnsOnlyCurrentBranchProducts()
    {
        using var fixture = CreateFixture();
        var branchA = await SeedBranchAsync(fixture.Context, "A");
        var branchB = await SeedBranchAsync(fixture.Context, "B");
        var user = await SeedUserAsync(fixture.Context, branchA.Id, "admin-a@test.local");

        await SeedProductAsync(fixture.Context, "SKU-A", branchA.Id);
        await SeedProductAsync(fixture.Context, "SKU-B", branchB.Id);

        var controller = new ProductsController(
            fixture.ProductService,
            fixture.NotifierMock.Object,
            fixture.Context);
        SetUserContext(controller, user.Id, UserRole.Admin);

        var action = await controller.GetAllProducts();
        var ok = Assert.IsType<OkObjectResult>(action);
        var items = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(ok.Value);

        var list = items.ToList();
        Assert.Single(list);
        Assert.Equal("SKU-A", list[0].SKU);
        Assert.Equal(branchA.Id, list[0].BranchId);
    }

    [Fact]
    public async Task ProductsController_CreateProduct_AutoAssignsCreatorBranch()
    {
        using var fixture = CreateFixture();
        var branchA = await SeedBranchAsync(fixture.Context, "A");
        var user = await SeedUserAsync(fixture.Context, branchA.Id, "admin-create@test.local");

        var controller = new ProductsController(
            fixture.ProductService,
            fixture.NotifierMock.Object,
            fixture.Context);
        SetUserContext(controller, user.Id, UserRole.Admin);

        var createDto = new CreateProductDto
        {
            Name = "Scoped Product",
            SKU = "SKU-CREATE-A",
            Price = 100m,
            StockQuantity = 12,
            Category = ProductCategory.TShirt
        };

        var action = await controller.CreateProduct(createDto);
        var created = Assert.IsType<CreatedAtActionResult>(action);
        var dto = Assert.IsType<ProductDto>(created.Value);

        Assert.Equal(branchA.Id, dto.BranchId);
        var stored = await fixture.Context.Products.AsNoTracking().SingleAsync(p => p.Id == dto.Id);
        Assert.Equal(branchA.Id, stored.BranchId);
    }

    [Fact]
    public async Task ErpInventoryController_GetAllInventoryItems_ReturnsOnlyCurrentBranchInventory()
    {
        using var fixture = CreateFixture();
        var branchA = await SeedBranchAsync(fixture.Context, "A");
        var branchB = await SeedBranchAsync(fixture.Context, "B");
        var user = await SeedUserAsync(fixture.Context, branchA.Id, "admin-inventory@test.local");

        await SeedProductAsync(fixture.Context, "INV-A", branchA.Id);
        await SeedProductAsync(fixture.Context, "INV-B", branchB.Id);

        var inventoryService = new ErpInventoryService(fixture.Context);
        var controller = new ErpInventoryController(inventoryService, fixture.NotifierMock.Object, fixture.Context);
        SetUserContext(controller, user.Id, UserRole.Admin);

        var action = await controller.GetAllInventoryItems();
        var ok = Assert.IsType<OkObjectResult>(action);
        var items = Assert.IsAssignableFrom<IEnumerable<InventoryItemDto>>(ok.Value);

        var list = items.ToList();
        Assert.Single(list);
        Assert.Equal("INV-A", list[0].SKU);
    }

    private static void SetUserContext(ControllerBase controller, int userId, UserRole role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("UserId", userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        ], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static async Task<Branch> SeedBranchAsync(ApplicationDbContext context, string suffix)
    {
        var branch = new Branch
        {
            Name = $"Branch {suffix}",
            Code = $"BR-{suffix}-{Guid.NewGuid():N}".Substring(0, 12),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<User> SeedUserAsync(ApplicationDbContext context, int branchId, string email)
    {
        var user = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = email,
            Phone = "09123456789",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            BranchId = branchId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Product> SeedProductAsync(ApplicationDbContext context, string sku, int branchId)
    {
        var product = new Product
        {
            Name = $"Product {sku}",
            SKU = sku,
            Price = 50m,
            StockQuantity = 10,
            MinStockLevel = 2,
            IsActive = true,
            Category = ProductCategory.TShirt,
            BranchId = branchId,
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

        var notifierMock = new Mock<IDataChangeNotifier>();
        var productLogger = new Mock<ILogger<ProductService>>();
        var productService = new ProductService(context, productLogger.Object);

        return new TestFixture(connection, context, notifierMock, productService);
    }

    private sealed class TestFixture : IDisposable
    {
        public TestFixture(
            SqliteConnection connection,
            ApplicationDbContext context,
            Mock<IDataChangeNotifier> notifierMock,
            ProductService productService)
        {
            Connection = connection;
            Context = context;
            NotifierMock = notifierMock;
            ProductService = productService;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public Mock<IDataChangeNotifier> NotifierMock { get; }
        public ProductService ProductService { get; }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }
}
