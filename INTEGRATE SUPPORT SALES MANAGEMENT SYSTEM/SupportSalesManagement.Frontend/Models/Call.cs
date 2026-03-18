namespace SupportSalesManagement.Frontend.Models
{
    public class Call
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int AgentId { get; set; }
        public User? Agent { get; set; }
        public string Type { get; set; } = "Inbound";
        public string Status { get; set; } = "Completed";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Duration { get; set; }
        public string? Subject { get; set; }
        public string? Notes { get; set; }
        public string? Outcome { get; set; }
        public bool IsEscalated { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Ticket> CreatedTickets { get; set; } = new();

        // Display helpers
        public string CustomerName => Customer != null ? $"{Customer.FirstName} {Customer.LastName}" : "Unknown";
        public string AgentName => Agent != null ? $"{Agent.FirstName} {Agent.LastName}" : "Unknown";
        public string DurationFormatted
        {
            get
            {
                if (EndTime.HasValue)
                {
                    var ts = EndTime.Value - StartTime;
                    return ts.ToString(@"hh\:mm\:ss");
                }
                return Duration ?? "00:00:00";
            }
        }
    }

    public class CreateCallRequest
    {
        public int CustomerId { get; set; }
        public int AgentId { get; set; }
        public string Type { get; set; } = "Inbound";
        public string? Subject { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCallRequest
    {
        public string? Status { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Subject { get; set; }
        public string? Notes { get; set; }
        public string? Outcome { get; set; }
        public bool? IsEscalated { get; set; }
    }

    public class CallSummary
    {
        public int TotalCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int MissedCalls { get; set; }
        public string AverageDuration { get; set; } = "00:00:00";
        public int EscalatedCalls { get; set; }
    }
}
