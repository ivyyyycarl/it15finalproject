using System.Threading.Tasks;

namespace SupportSalesManagement.Frontend.Services
{
    /// <summary>
    /// Service for interacting with the ERP Finance Module API.
    /// Used for Inventory Valuation and Transaction Logging.
    /// </summary>
    public interface IFinanceService
    {
        /// <summary>
        /// Gets the current total valuation of all inventory assets.
        /// </summary>
        Task<decimal> GetInventoryValuationAsync();

        /// <summary>
        /// Logs a material inventory transaction for financial audit.
        /// </summary>
        Task LogTransactionAsync(TransactionDto transaction);
    }

    public class TransactionDto
    {
        public required string ProductId { get; set; }
        public required string Type { get; set; } // "Restock", "Adjustment", "Sale"
        public int QuantityChange { get; set; }
        public decimal UnitCost { get; set; }
        public required string UserId { get; set; }
        public required string Description { get; set; }
    }
}
