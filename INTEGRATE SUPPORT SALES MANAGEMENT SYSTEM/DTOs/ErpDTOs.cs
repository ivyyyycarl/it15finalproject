using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    // ============================================
    // ERP INVENTORY DTOs
    // ============================================

    public class InventoryItemDto
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? Style { get; set; }
        public int StockQuantity { get; set; }
        public string? WarehouseLocation { get; set; }
        public int ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? LastRestockDate { get; set; }
        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }

    public class UpdateInventoryStockDto
    {
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }

    // ============================================
    // ERP FINANCE DTOs
    // ============================================

    public class FinancialTransactionDto
    {
        public int Id { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PHP";
        public DateTime TransactionDate { get; set; }
        public int? OrderId { get; set; }
        public int? PaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? Description { get; set; }
    }

    public class CreateFinancialTransactionDto
    {
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PHP";
        public int? OrderId { get; set; }
        public int? PaymentId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Description { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInvoiceDto
    {
        public int? OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class RecordPaymentDto
    {
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
    }

    public class FinancialSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
        public decimal PendingInvoicesAmount { get; set; }
        public int PendingInvoicesCount { get; set; }
        public decimal PaidInvoicesAmount { get; set; }
        public int PaidInvoicesCount { get; set; }
        public decimal OverdueInvoicesAmount { get; set; }
        public int OverdueInvoicesCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
