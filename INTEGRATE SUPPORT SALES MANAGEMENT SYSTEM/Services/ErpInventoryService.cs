using System;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class ErpInventoryService : IErpInventoryService
    {
        private readonly ApplicationDbContext _context;

        public ErpInventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryItemDto>> GetAllInventoryItemsAsync(int? branchId)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var products = await query.OrderBy(p => p.Name).ToListAsync();
            return products.Select(MapProductToDto).ToList();
        }

        public async Task<InventoryItemDto?> GetInventoryItemBySKUAsync(string sku, int? branchId)
        {
            var normalizedSku = sku.ToLower();
            var query = _context.Products.Where(p => p.SKU.ToLower() == normalizedSku);
            if (branchId.HasValue)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var product = await query.FirstOrDefaultAsync();
            return product != null ? MapProductToDto(product) : null;
        }

        public async Task<List<InventoryItemDto>> GetInventoryItemsByCategoryAsync(string category, int? branchId)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var products = await query.ToListAsync();

            return products
                .Where(p => p.Category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase))
                .Select(MapProductToDto)
                .ToList();
        }

        public async Task<List<InventoryItemDto>> GetLowStockItemsAsync(int? branchId)
        {
            var query = _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                .AsQueryable();
            if (branchId.HasValue)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var products = await query.ToListAsync();
            return products.Select(MapProductToDto).ToList();
        }

        public async Task<bool> UpdateStockAsync(string sku, UpdateInventoryStockDto updateDto, int? branchId)
        {
            var normalizedSku = sku.ToLower();
            var query = _context.Products.Where(p => p.SKU.ToLower() == normalizedSku);
            if (branchId.HasValue)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var product = await query.FirstOrDefaultAsync();
            if (product == null) return false;

            product.StockQuantity = updateDto.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static InventoryItemDto MapProductToDto(Product product)
        {
            return new InventoryItemDto
            {
                Id = product.Id,
                SKU = product.SKU,
                ProductName = product.Name,
                Category = product.Category.ToString(),
                StockQuantity = product.StockQuantity,
                ReorderLevel = product.MinStockLevel,
                UnitCost = product.Price,
                LastRestockDate = product.UpdatedAt ?? product.CreatedAt
            };
        }
    }
}
