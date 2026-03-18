using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    /// <summary>
    /// In-memory model used by the ERP inventory simulation service.
    /// Not stored in the database (not registered in ApplicationDbContext).
    /// </summary>
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(50)]
        public string? Style { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(100)]
        public string? WarehouseLocation { get; set; }

        public int ReorderLevel { get; set; } = 10;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        public DateTime? LastRestockDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
