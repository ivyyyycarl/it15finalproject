namespace SupportSalesManagement.Frontend.Models
{
    public class Ticket
    {
        private List<string> _tags = new();
        private List<TicketComment> _comments = new();

        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty; // Added for agent dashboard
        public string Description { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty; // Added for display
        public Customer? Customer { get; set; }
        public int? AssignedAgentId { get; set; }
        public string AssignedTo { get; set; } = string.Empty; // Added for display
        public User? AssignedAgent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime CreatedDate { get; set; } // Added for consistency
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? RelatedCallId { get; set; }
        public string? Resolution { get; set; }
        public List<string> Tags
        {
            get => _tags;
            set => _tags = value ?? new();
        } // Added for agent dashboard filtering
        public List<TicketComment> Comments
        {
            get => _comments;
            set => _comments = value ?? new();
        }
    }

    public class TicketComment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTicketRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Category { get; set; } = "General";
        public int? AssignedAgentId { get; set; }
        public int? RelatedCallId { get; set; }
    }

    public class UpdateTicketRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int? AssignedAgentId { get; set; }
    }

    public class AddCommentRequest
    {
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }
}
