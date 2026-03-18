using System.ComponentModel.DataAnnotations;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    // Ticket DTOs
    public class TicketDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }
        public int? AssignedAgentId { get; set; }
        public UserDto? AssignedAgent { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public TicketCategory Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
        public int? RelatedCallId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AssignedAgentName { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public List<TicketCommentDto> Comments { get; set; } = new();
    }
    
    public class CreateTicketDto
    {
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        
        public TicketCategory Category { get; set; } = TicketCategory.General;
        
        public int? AssignedAgentId { get; set; }

        public int? CreatedByUserId { get; set; }
        
        public int? RelatedCallId { get; set; }
    }
    
    public class UpdateTicketDto
    {
        [StringLength(200)]
        public string? Title { get; set; }
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        public TicketPriority? Priority { get; set; }
        
        public TicketStatus? Status { get; set; }
        
        public TicketCategory? Category { get; set; }
        
        public int? AssignedAgentId { get; set; }
        
        [StringLength(1000)]
        public string? Resolution { get; set; }
    }
    
    public class TicketCommentDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class CreateTicketCommentDto
    {
        [Required]
        public int TicketId { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Comment { get; set; } = string.Empty;
        
        public bool IsInternal { get; set; } = false;
    }
}
