using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    // Order DTOs
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }
        public int? AgentId { get; set; }
        public UserDto? Agent { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public int? RelatedCallId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ItemCount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? PaymentIntentId { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; } = new();
    }
    
    public class CreateOrderDto
    {
        [Required]
        public int CustomerId { get; set; }
        
        public int? AgentId { get; set; }
        
        public int? RelatedCallId { get; set; }
        
        [Required]
        public List<CreateOrderDetailDto> OrderDetails { get; set; } = new();
        
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        
        [StringLength(500)]
        public string? ShippingAddress { get; set; }
        
        [StringLength(500)]
        public string? BillingAddress { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }

        public string? PaymentIntentId { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    }
    
    public class UpdateOrderDto
    {
        public int? CustomerId { get; set; }
        public int? AgentId { get; set; }
        public int? RelatedCallId { get; set; }
        public OrderStatus? Status { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        
        [StringLength(500)]
        public string? ShippingAddress { get; set; }
        
        [StringLength(500)]
        public string? BillingAddress { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
    
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public ProductDto? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class CreateOrderDetailDto
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }
        
        public decimal UnitPrice { get; set; }
    }
    
    public class ProductDto
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public bool IsSubscription { get; set; }
        public int? SubscriptionMonths { get; set; }
        public ProductCategory Category { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ImageUrl { get; set; }
    }
}
