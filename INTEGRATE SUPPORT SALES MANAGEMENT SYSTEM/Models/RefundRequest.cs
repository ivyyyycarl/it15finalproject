using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class RefundRequest
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int RequestedByUserId { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = RefundRequestStatus.Pending;

        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
    }

    public static class RefundRequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}
