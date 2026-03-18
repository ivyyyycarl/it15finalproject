using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public enum ChannelType
    {
        Chat = 1,
        Email = 2,
        SocialMedia = 3
    }

    public class ChannelInteractionDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AgentId { get; set; }
        public ChannelType Channel { get; set; }
        public string? Subject { get; set; }
        public string? Notes { get; set; }
        public string? Outcome { get; set; }
        public bool IsEscalated { get; set; }
        public CallStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class CreateChannelInteractionDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int AgentId { get; set; }

        [Required]
        public ChannelType Channel { get; set; }

        [StringLength(1000)]
        public string? Subject { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    public class ResolveChannelInteractionDto
    {
        [StringLength(100)]
        public string? Outcome { get; set; }

        public bool IsEscalated { get; set; }
    }
}
