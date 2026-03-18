using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class FinancialTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionNumber { get; set; } = string.Empty;

        public TransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "PHP";

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public int? OrderId { get; set; }

        public int? PaymentId { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual Payment? Payment { get; set; }
    }

    public enum TransactionType
    {
        Sale = 1,
        Refund = 2,
        Expense = 3,
        Payment = 4,
        Adjustment = 5
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }
}
