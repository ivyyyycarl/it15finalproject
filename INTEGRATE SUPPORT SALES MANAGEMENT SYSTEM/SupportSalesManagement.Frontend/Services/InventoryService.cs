using System.Collections.Generic;
using System.Threading.Tasks;

namespace SupportSalesManagement.Frontend.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApiClient _apiClient;

        public InventoryService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                if (products == null) return new List<ProductDto>();

                return products.Select(p => new ProductDto
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Size = "",
                    Color = "",
                    Price = p.Price,
                    Stock = p.StockQuantity,
                    ImageUrl = p.ImageUrl ?? ""
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.GetProductsAsync error: {ex.Message}");
                return new List<ProductDto>();
            }
        }

        public async Task<InventoryStatsDto> GetStatsAsync()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                var productList = products ?? new List<Models.Product>();

                return new InventoryStatsDto
                {
                    TotalItems = productList.Sum(p => p.StockQuantity),
                    LowStockCount = productList.Count(p => p.StockQuantity <= p.MinStockLevel),
                    IncomingUnits = 0,
                    TotalValue = productList.Sum(p => p.StockQuantity * p.Price)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.GetStatsAsync error: {ex.Message}");
                return new InventoryStatsDto();
            }
        }

        public async Task<List<InventoryTransactionDto>> GetTransactionsAsync()
        {
            try
            {
                var transactions = await _apiClient.GetErpTransactionsAsync();
                if (transactions == null) return new List<InventoryTransactionDto>();

                return transactions.Select(t => new InventoryTransactionDto
                {
                    Timestamp = t.TransactionDate,
                    Type = t.Type,
                    Description = t.Description ?? "",
                    User = t.PaymentMethod ?? "System",
                    QuantityChange = (int)t.Amount
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.GetTransactionsAsync error: {ex.Message}");
                return new List<InventoryTransactionDto>();
            }
        }

        public async Task UpdateStockAsync(string productId, int quantity, string reason)
        {
            try
            {
                if (int.TryParse(productId, out var id))
                {
                    await _apiClient.UpdateProductAsync(id, new Models.UpdateProductRequest
                    {
                        StockQuantity = quantity
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.UpdateStockAsync error: {ex.Message}");
            }
        }

        public async Task AddProductAsync(ProductDto product)
        {
            try
            {
                await _apiClient.CreateProductAsync(new Models.CreateProductRequest
                {
                    Name = product.Name,
                    SKU = $"PRD-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    Price = product.Price,
                    StockQuantity = product.Stock,
                    ImageUrl = product.ImageUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.AddProductAsync error: {ex.Message}");
            }
        }

        public async Task ArchiveProductAsync(string productId)
        {
            try
            {
                if (int.TryParse(productId, out var id))
                {
                    await _apiClient.DeleteProductAsync(id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InventoryService.ArchiveProductAsync error: {ex.Message}");
            }
        }
    }
}
