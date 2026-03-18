using System;
using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? AssignedAgentId { get; set; }

        public int? CreatedByUserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public TicketCategory Category { get; set; } = TicketCategory.General;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [StringLength(1000)]
        public string? Resolution { get; set; }

        public int? RelatedCallId { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual User? AssignedAgent { get; set; }
        public virtual User? CreatedByAgent { get; set; }
        public virtual Call? RelatedCall { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; } = [];
    }

    public enum TicketStatus
    {
        Open = 1,
        InProgress = 2,
        PendingCustomer = 3,
        Resolved = 4,
        Closed = 5,
        Reopened = 6
    }

    public enum TicketPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TicketCategory
    {
        General = 1,
        Technical = 2,
        Billing = 3,
        Account = 4,
        Product = 5,
        Service = 6
    }
}
