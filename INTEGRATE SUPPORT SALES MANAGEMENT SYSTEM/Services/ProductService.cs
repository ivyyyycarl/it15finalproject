using System;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
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
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(product => new ProductDto
                {
                    Id = product.Id,
                    BranchId = product.BranchId,
                    Name = product.Name,
                    Description = product.Description,
                    SKU = product.SKU,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    Category = product.Category,
                    IsActive = product.IsActive,
                    MinStockLevel = product.MinStockLevel,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ImageUrl = product.ImageUrl
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.SKU.ToLower() == createProductDto.SKU.ToLower() &&
                    p.BranchId == createProductDto.BranchId);

            if (existingProduct != null)
            {
                throw new InvalidOperationException("Product with this SKU already exists");
            }

            var product = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                SKU = createProductDto.SKU,
                Price = createProductDto.Price,
                StockQuantity = createProductDto.StockQuantity,
                Category = createProductDto.Category,
                BranchId = createProductDto.BranchId,

                IsActive = true,
                MinStockLevel = createProductDto.MinStockLevel,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = createProductDto.ImageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return (await GetProductByIdAsync(product.Id))!;
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            if (!string.IsNullOrEmpty(updateProductDto.Name))
                product.Name = updateProductDto.Name;

            if (updateProductDto.Description != null)
                product.Description = updateProductDto.Description;

            if (!string.IsNullOrEmpty(updateProductDto.SKU))
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.SKU.ToLower() == updateProductDto.SKU.ToLower() &&
                        p.Id != id &&
                        p.BranchId == product.BranchId);

                if (existingProduct != null)
                {
                    throw new InvalidOperationException("SKU is already taken by another product");
                }

                product.SKU = updateProductDto.SKU;
            }

            if (updateProductDto.Price.HasValue)
                product.Price = updateProductDto.Price.Value;

            if (updateProductDto.StockQuantity.HasValue)
                product.StockQuantity = updateProductDto.StockQuantity.Value;

            if (updateProductDto.MinStockLevel.HasValue)
                product.MinStockLevel = updateProductDto.MinStockLevel.Value;

            if (!string.IsNullOrWhiteSpace(updateProductDto.Category))
            {
                if (Enum.TryParse<ProductCategory>(updateProductDto.Category, true, out var parsedCategory))
                {
                    product.Category = parsedCategory;
                }
            }

            if (updateProductDto.IsActive.HasValue)
                product.IsActive = updateProductDto.IsActive.Value;

            if (updateProductDto.ImageUrl != null)
                product.ImageUrl = updateProductDto.ImageUrl;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetProductByIdAsync(id);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return false;

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(ProductCategory category)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Category == category && p.IsActive)
                .OrderBy(p => p.Name)
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
        }

        public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
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
        }

        public async Task<ProductDto?> GetProductBySKUAsync(string sku)
        {
            var normalizedSku = sku.ToLower();
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.SKU.ToLower() == normalizedSku)
                .Select(product => new ProductDto
                {
                    Id = product.Id,
                    BranchId = product.BranchId,
                    Name = product.Name,
                    Description = product.Description,
                    SKU = product.SKU,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    Category = product.Category,
                    IsActive = product.IsActive,
                    MinStockLevel = product.MinStockLevel,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ImageUrl = product.ImageUrl
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity <= p.MinStockLevel && p.IsActive)
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
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
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return false;

            product.StockQuantity = quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
