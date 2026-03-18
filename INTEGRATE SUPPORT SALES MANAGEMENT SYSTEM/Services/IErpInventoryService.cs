using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IErpInventoryService
    {
        Task<List<InventoryItemDto>> GetAllInventoryItemsAsync(int? branchId);
        Task<InventoryItemDto?> GetInventoryItemBySKUAsync(string sku, int? branchId);
        Task<List<InventoryItemDto>> GetInventoryItemsByCategoryAsync(string category, int? branchId);
        Task<List<InventoryItemDto>> GetLowStockItemsAsync(int? branchId);
        Task<bool> UpdateStockAsync(string sku, UpdateInventoryStockDto updateDto, int? branchId);
    }
}
