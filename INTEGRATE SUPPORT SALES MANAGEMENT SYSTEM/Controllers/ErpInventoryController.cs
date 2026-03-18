using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/erp/inventory")]
    [Authorize]
    public class ErpInventoryController : ControllerBase
    {
        private readonly IErpInventoryService _inventoryService;
        private readonly IDataChangeNotifier _notifier;
        private readonly ApplicationDbContext _context;

        public ErpInventoryController(IErpInventoryService inventoryService, IDataChangeNotifier notifier, ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _notifier = notifier;
            _context = context;
        }

        /// <summary>
        /// Get all inventory items from ERP system
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            var branchId = await GetScopeBranchIdAsync();
            if (!IsSuperAdmin() && !branchId.HasValue)
            {
                return Ok(new List<InventoryItemDto>());
            }

            var items = await _inventoryService.GetAllInventoryItemsAsync(branchId);
            return Ok(items);
        }

        /// <summary>
        /// Get inventory item by SKU
        /// </summary>
        [HttpGet("{sku}")]
        public async Task<IActionResult> GetInventoryItemBySKU(string sku)
        {
            var branchId = await GetScopeBranchIdAsync();
            if (!IsSuperAdmin() && !branchId.HasValue)
            {
                return NotFound(new { message = $"Inventory item with SKU '{sku}' not found" });
            }

            var item = await _inventoryService.GetInventoryItemBySKUAsync(sku, branchId);
            if (item == null)
            {
                return NotFound(new { message = $"Inventory item with SKU '{sku}' not found" });
            }

            return Ok(item);
        }

        /// <summary>
        /// Get inventory items by category
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetInventoryItemsByCategory(string category)
        {
            var branchId = await GetScopeBranchIdAsync();
            if (!IsSuperAdmin() && !branchId.HasValue)
            {
                return Ok(new List<InventoryItemDto>());
            }

            var items = await _inventoryService.GetInventoryItemsByCategoryAsync(category, branchId);
            return Ok(items);
        }

        /// <summary>
        /// Get low stock inventory items (below reorder level)
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockItems()
        {
            var branchId = await GetScopeBranchIdAsync();
            if (!IsSuperAdmin() && !branchId.HasValue)
            {
                return Ok(new List<InventoryItemDto>());
            }

            var items = await _inventoryService.GetLowStockItemsAsync(branchId);
            return Ok(items);
        }

        /// <summary>
        /// Update stock quantity for an inventory item
        /// </summary>
        [HttpPut("{sku}/stock")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateStock(string sku, [FromBody] UpdateInventoryStockDto updateDto)
        {
            var branchId = await GetScopeBranchIdAsync();
            if (!IsSuperAdmin() && !branchId.HasValue)
            {
                return Forbid();
            }

            var result = await _inventoryService.UpdateStockAsync(sku, updateDto, branchId);
            if (!result)
            {
                return NotFound(new { message = $"Inventory item with SKU '{sku}' not found" });
            }

            await _notifier.NotifyDataChanged("Inventory", "StockUpdated");
            return Ok(new { message = "Stock updated successfully", sku, newQuantity = updateDto.Quantity });
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<int?> GetScopeBranchIdAsync()
        {
            if (IsSuperAdmin())
            {
                return null;
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            var branchId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            return branchId;
        }
    }
}
