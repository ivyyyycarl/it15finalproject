namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class SystemStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveAdmins { get; set; }
        public int TotalTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalCalls { get; set; }
        public double AverageResponseTimeHours { get; set; }
        public double SystemUptime { get; set; } = 0;
    }
}
