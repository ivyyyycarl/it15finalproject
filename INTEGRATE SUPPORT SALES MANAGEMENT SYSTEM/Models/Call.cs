using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class Call
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int AgentId { get; set; }

        public CallType Type { get; set; } = CallType.Inbound;

        public CallStatus Status { get; set; } = CallStatus.Completed;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        [StringLength(1000)]
        public string? Subject { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Outcome { get; set; }

        public bool IsEscalated { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual User Agent { get; set; } = null!;
        public virtual ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    }

    public enum CallType
    {
        Inbound = 1,
        Outbound = 2,
        FollowUp = 3,
        Chat = 4,
        Email = 5,
        SocialMedia = 6
    }

    public enum CallStatus
    {
        Scheduled = 1,
        InProgress = 2,
        Completed = 3,
        Missed = 4,
        Cancelled = 5
    }
}
