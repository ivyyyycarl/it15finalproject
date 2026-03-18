using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> GetActiveProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductBySKUAsync(string sku);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
    }

    public class CreateProductDto
    {
        public int? BranchId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsSubscription { get; set; } = false;

        public int? SubscriptionMonths { get; set; }

        public ProductCategory Category { get; set; } = ProductCategory.TShirt;

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int MinStockLevel { get; set; } = 5;

        [StringLength(2000)]
        public string? ImageUrl { get; set; }
    }

    public class UpdateProductDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsSubscription { get; set; }

        public int? SubscriptionMonths { get; set; }

        public string? Category { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int? MinStockLevel { get; set; }

        [StringLength(2000)]
        public string? ImageUrl { get; set; }
    }
}
