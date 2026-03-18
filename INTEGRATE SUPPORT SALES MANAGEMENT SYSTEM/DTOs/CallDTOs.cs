using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    // Call DTOs
    public class CallDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }
        public int AgentId { get; set; }
        public UserDto? Agent { get; set; }
        public CallType Type { get; set; }
        public CallStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Subject { get; set; }
        public string? Notes { get; set; }
        public string? Outcome { get; set; }
        public bool IsEscalated { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TicketDto> CreatedTickets { get; set; } = new();
    }
    
    public class CreateCallDto
    {
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        public int AgentId { get; set; }
        
        public CallType Type { get; set; } = CallType.Inbound;
        
        [StringLength(1000)]
        public string? Subject { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
    }
    
    public class UpdateCallDto
    {
        public CallStatus? Status { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        [StringLength(1000)]
        public string? Subject { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        public string? Outcome { get; set; }
        
        public bool? IsEscalated { get; set; }
    }
    
    public class CallSummaryDto
    {
        public int TotalCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int MissedCalls { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public int EscalatedCalls { get; set; }
    }
}
