using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class PerformanceReport
    {
        public int Id { get; set; }

        [Required]
        public int AgentId { get; set; }

        public int TicketsResolved { get; set; }

        public TimeSpan AvgHandlingTime { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SalesConversionRate { get; set; }

        public int TotalCallsHandled { get; set; }
        public TimeSpan TotalCallDuration { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal ResolutionRate { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Agent { get; set; } = null!;
    }
}
