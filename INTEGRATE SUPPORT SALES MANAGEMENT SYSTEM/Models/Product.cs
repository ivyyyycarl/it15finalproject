using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsSubscription { get; set; } = false;

        public int? SubscriptionMonths { get; set; }

        public ProductCategory Category { get; set; } = ProductCategory.TShirt;

        public int StockQuantity { get; set; } = 0;

        public int MinStockLevel { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(2000)]
        public string? ImageUrl { get; set; }

        public int? BranchId { get; set; }

        public virtual Branch? Branch { get; set; }

        // Navigation properties
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum ProductCategory
    {
        TShirt = 1,
        Dress = 2,
        Jacket = 3,
        Sweater = 4
    }
}
