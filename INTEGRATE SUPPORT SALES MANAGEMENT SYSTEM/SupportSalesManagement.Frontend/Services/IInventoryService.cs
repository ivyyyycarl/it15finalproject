using System.Threading.Tasks;
using System.Collections.Generic;

namespace SupportSalesManagement.Frontend.Services
{
    /// <summary>
    /// Service for interacting with the ERP Inventory Module API.
    /// Implementation should handle authentication and serialization.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Retrieves the full product catalog with current stock levels.
        /// </summary>
        Task<List<ProductDto>> GetProductsAsync();

        /// <summary>
        /// Retrieves inventory statistics (Total Value, Low Stock Count, etc.).
        /// </summary>
        Task<InventoryStatsDto> GetStatsAsync();

        /// <summary>
        /// Retrieves recent stock usage transactions.
        /// </summary>
        Task<List<InventoryTransactionDto>> GetTransactionsAsync();

        /// <summary>
        /// Updates the stock quantity for a specific product.
        /// </summary>
        /// <param name="productId">The unique product identifier (e.g., PRD-001)</param>
        /// <param name="quantity">The new stock quantity</param>
        /// <param name="reason">Reason for adjustment (for audit logs)</param>
        Task UpdateStockAsync(string productId, int quantity, string reason);

        /// <summary>
        /// Adds a new product to the inventory catalog.
        /// </summary>
        Task AddProductAsync(ProductDto product);

        /// <summary>
        /// Archives or deletes a product from the active catalog.
        /// </summary>
        Task ArchiveProductAsync(string productId);
    }

    public class ProductDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; } = "";
    }

    public class InventoryStatsDto
    {
        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public int IncomingUnits { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class InventoryTransactionDto
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string User { get; set; } = "";
        public int QuantityChange { get; set; }
    }
}
