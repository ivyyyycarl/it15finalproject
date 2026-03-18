using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class PerformanceReportFilterDto
    {
        public int? AgentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AgentPerformanceReportDto
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int TotalCallsHandled { get; set; }
        public int CompletedCalls { get; set; }
        public TimeSpan AverageHandlingTime { get; set; }
        public int TicketsResolved { get; set; }
        public decimal ResolutionRate { get; set; }
        public int OrdersProcessed { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal SalesConversionRate { get; set; }
    }

    public class ExportReportQueryDto
    {
        [Required]
        public string Format { get; set; } = "csv";

        public int? AgentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
