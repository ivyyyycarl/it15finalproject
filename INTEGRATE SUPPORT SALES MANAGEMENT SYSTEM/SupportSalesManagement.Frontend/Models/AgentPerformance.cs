namespace SupportSalesManagement.Frontend.Models
{
    public class AgentPerformance
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline"; // Available, Busy, On Call, Offline
        public int TicketsHandledToday { get; set; }
        public int ActiveTickets { get; set; }
        public string AverageResponseTime { get; set; } = "0h";
        public decimal CustomerSatisfaction { get; set; } // 0.0 to 5.0
        public decimal SalesConversionRate { get; set; } // 0.0 to 1.0 (percentage)
        public int TotalTicketsHandled { get; set; }
        public int CallsToday { get; set; }
        public string AverageCallDuration { get; set; } = "0m";
        public decimal ResolutionRate { get; set; } // 0.0 to 1.0
        public DateTime LastActivityTime { get; set; }
    }

    public class SupervisorStats
    {
        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int EscalatedTickets { get; set; }
        public int OverdueTickets { get; set; }
        public decimal TeamAverageHandlingTime { get; set; }
        public decimal TeamCustomerSatisfaction { get; set; }
        public decimal TeamSalesConversion { get; set; }
        public int TotalCallsToday { get; set; }
    }
}
