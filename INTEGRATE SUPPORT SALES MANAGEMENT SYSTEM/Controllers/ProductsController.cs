using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using System.Security.Claims;
using System.Linq.Expressions;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IDataChangeNotifier _notifier;
        private readonly ApplicationDbContext _context;
        
        public ProductsController(IProductService productService, IDataChangeNotifier notifier, ApplicationDbContext context)
        {
            _productService = productService;
            _notifier = notifier;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var query = await BuildScopedProductQueryAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Select(MapToProductDto())
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetProductsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null,
            [FromQuery] ProductCategory? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool lowStockOnly = false,
            [FromQuery] string? stockStatus = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortDir = "asc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = await BuildScopedProductQueryAsync();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (lowStockOnly)
            {
                query = query.Where(p => p.StockQuantity <= p.MinStockLevel);
            }

            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                var normalizedStockStatus = stockStatus.Trim().ToLowerInvariant();
                if (normalizedStockStatus == "low")
                {
                    query = query.Where(p => p.StockQuantity <= p.MinStockLevel);
                }
                else if (normalizedStockStatus == "out")
                {
                    query = query.Where(p => p.StockQuantity == 0);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "price" => isDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "stock" => isDesc ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                "category" => isDesc ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                "createdat" => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                "updatedat" => isDesc ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
                "sku" => isDesc ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
                _ => isDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    BranchId = p.BranchId,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Category = p.Category,
                    IsActive = p.IsActive,
                    MinStockLevel = p.MinStockLevel,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Ok(PagedResultDto<ProductDto>.Create(items, page, pageSize, totalCount));
        }
        
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProducts()
        {
            var query = await BuildScopedProductQueryAsync();
            var products = await query
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(MapToProductDto())
                .ToListAsync();
            return Ok(products);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var query = await BuildScopedProductQueryAsync();
            var product = await query
                .Where(p => p.Id == id)
                .Select(MapToProductDto())
                .FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(product);
        }
        
        [HttpGet("sku/{sku}")]
        public async Task<IActionResult> GetProductBySKU(string sku)
        {
            var normalizedSku = sku.Trim().ToLowerInvariant();
            var query = await BuildScopedProductQueryAsync();
            var product = await query
                .Where(p => p.SKU.ToLower() == normalizedSku)
                .Select(MapToProductDto())
                .FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(product);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            var branchId = await GetCurrentBranchIdAsync();
            
            // If the user is an Admin assigned to a branch, auto-assign their branch 
            // (overriding whatever they sent)
            if (!IsSuperAdmin() && branchId.HasValue)
            {
                createProductDto.BranchId = branchId.Value;
            }
            // If the user is SuperAdmin OR an Admin NOT assigned to a branch, 
            // they must provide a valid BranchId (or null if the system allows global products)
            else if (createProductDto.BranchId.HasValue)
            {
                var branchExists = await _context.Branches.AsNoTracking().AnyAsync(b => b.Id == createProductDto.BranchId.Value);
                if (!branchExists)
                {
                    return BadRequest(new { message = "Selected branch does not exist." });
                }
            }

            var normalizedSku = createProductDto.SKU.Trim().ToLowerInvariant();
            var duplicateExists = await _context.Products
                .AsNoTracking()
                .AnyAsync(p => p.SKU.ToLower() == normalizedSku && p.BranchId == createProductDto.BranchId);
            if (duplicateExists)
            {
                return Conflict(new { message = "Product with this SKU already exists" });
            }

            try
            {
                var product = await _productService.CreateProductAsync(createProductDto);
                await _notifier.NotifyDataChanged("Product", "Created");
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateSkuException(ex))
            {
                return Conflict(new { message = "Product with this SKU already exists" });
            }
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            if (!await CanAccessProductAsync(id))
            {
                return NotFound(new { message = "Product not found" });
            }

            try
            {
                var product = await _productService.UpdateProductAsync(id, updateProductDto);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                await _notifier.NotifyDataChanged("Product", "Updated");
                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product.", detail = ex.Message });
            }
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!await CanAccessProductAsync(id))
            {
                return NotFound(new { message = "Product not found" });
            }

            var result = await _productService.DeleteProductAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Product not found" });
            }

            await _notifier.NotifyDataChanged("Product", "Deleted");
            return NoContent();
        }
        
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto updateStockDto)
        {
            if (!await CanAccessProductAsync(id))
            {
                return NotFound(new { message = "Product not found" });
            }

            var result = await _productService.UpdateStockAsync(id, updateStockDto.Quantity);
            if (!result)
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(new { message = "Stock updated successfully" });
        }
        
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            var query = await BuildScopedProductQueryAsync();
            var products = await query
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .Select(MapToProductDto())
                .ToListAsync();
            return Ok(products);
        }

        private async Task<IQueryable<Product>> BuildScopedProductQueryAsync()
        {
            var query = _context.Products.AsNoTracking().AsQueryable();
            if (IsSuperAdmin())
            {
                return query;
            }

            var branchId = await GetCurrentBranchIdAsync();
            if (!branchId.HasValue)
            {
                // Allow unassigned customers to browse products and choose a branch at checkout.
                // Order creation already enforces single-branch checkout and auto-assigns branch.
                if (IsCustomer())
                {
                    return query.Where(p => p.IsActive);
                }

                return query.Where(_ => false);
            }

            // For customers, include legacy products with no branch to avoid empty catalog during migration.
            if (IsCustomer())
            {
                return query.Where(p => p.BranchId == branchId.Value || !p.BranchId.HasValue);
            }

            return query.Where(p => p.BranchId == branchId.Value);
        }

        private async Task<bool> CanAccessProductAsync(int productId)
        {
            // SuperAdmins and Admins can modify any product regardless of branch scope
            if (IsSuperAdmin() || User.IsInRole("Admin"))
            {
                return await _context.Products.AsNoTracking().AnyAsync(p => p.Id == productId);
            }

            var query = await BuildScopedProductQueryAsync();
            return await query.AnyAsync(p => p.Id == productId);
        }

        private static Expression<Func<Product, ProductDto>> MapToProductDto()
        {
            return p => new ProductDto
            {
                Id = p.Id,
                BranchId = p.BranchId,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Category = p.Category,
                IsActive = p.IsActive,
                MinStockLevel = p.MinStockLevel,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ImageUrl = p.ImageUrl
            };
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");
        private bool IsCustomer() => User.IsInRole("Customer");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentBranchIdAsync()
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

        private static bool IsDuplicateSkuException(DbUpdateException ex)
        {
            if (ex.InnerException is not SqlException sqlException)
            {
                return false;
            }

            return (sqlException.Number == 2601 || sqlException.Number == 2627) &&
                   (sqlException.Message.Contains("IX_Products_SKU", StringComparison.OrdinalIgnoreCase) ||
                    sqlException.Message.Contains("IX_Products_BranchId_SKU", StringComparison.OrdinalIgnoreCase));
        }
    }
    
    public class UpdateStockDto
    {
        public int Quantity { get; set; }
    }
}
