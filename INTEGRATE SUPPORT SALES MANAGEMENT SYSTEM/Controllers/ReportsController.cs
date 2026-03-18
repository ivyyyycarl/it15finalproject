using System.Text;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("performance")]
        [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> GetPerformanceReport([FromQuery] PerformanceReportFilterDto filter)
        {
            var data = await BuildPerformanceReportAsync(filter);
            return Ok(data);
        }

        [HttpGet("performance/export")]
        [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
        public async Task<IActionResult> ExportPerformanceReport([FromQuery] ExportReportQueryDto query)
        {
            var filter = new PerformanceReportFilterDto
            {
                AgentId = query.AgentId,
                StartDate = query.StartDate,
                EndDate = query.EndDate
            };
            var data = await BuildPerformanceReportAsync(filter);

            var format = query.Format.Trim().ToLowerInvariant();
            if (format == "csv")
            {
                var csv = BuildCsv(data);
                var fileName = $"performance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }

            if (format == "pdf")
            {
                var pdf = BuildSimplePdf(data);
                var fileName = $"performance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(pdf, "application/pdf", fileName);
            }

            return BadRequest(new { message = "Unsupported format. Use 'csv' or 'pdf'." });
        }

        private async Task<List<AgentPerformanceReportDto>> BuildPerformanceReportAsync(PerformanceReportFilterDto filter)
        {
            var startDate = filter.StartDate?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
            var endDate = (filter.EndDate?.Date.AddDays(1).AddTicks(-1)) ?? DateTime.UtcNow;

            var agentQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Agent && u.IsActive);

            if (!User.IsInRole("SuperAdmin"))
            {
                var currentBranchId = await GetCurrentUserBranchIdAsync();
                if (!currentBranchId.HasValue)
                {
                    return new List<AgentPerformanceReportDto>();
                }

                agentQuery = agentQuery.Where(u => u.BranchId == currentBranchId.Value);
            }

            if (filter.AgentId.HasValue)
            {
                agentQuery = agentQuery.Where(u => u.Id == filter.AgentId.Value);
            }

            var agents = await agentQuery
                .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                .ToListAsync();

            var results = new List<AgentPerformanceReportDto>();
            foreach (var agent in agents)
            {
                var callsQuery = _context.Calls
                    .AsNoTracking()
                    .Where(c => c.AgentId == agent.Id && c.StartTime >= startDate && c.StartTime <= endDate);

                var totalCalls = await callsQuery.CountAsync();
                var completedCalls = await callsQuery.CountAsync(c => c.Status == CallStatus.Completed);
                var averageHandlingTicks = await callsQuery
                    .Where(c => c.EndTime.HasValue && c.EndTime > c.StartTime)
                    .Select(c => (c.EndTime!.Value - c.StartTime).Ticks)
                    .DefaultIfEmpty(0L)
                    .AverageAsync();

                var ticketsQuery = _context.Tickets
                    .AsNoTracking()
                    .Where(t => t.AssignedAgentId == agent.Id && t.CreatedAt >= startDate && t.CreatedAt <= endDate);
                var totalTickets = await ticketsQuery.CountAsync();
                var resolvedTickets = await ticketsQuery.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
                var resolutionRate = totalTickets == 0 ? 0m : Math.Round((decimal)resolvedTickets / totalTickets * 100m, 2);

                var ordersQuery = _context.Orders
                    .AsNoTracking()
                    .Where(o => o.AgentId == agent.Id && o.OrderDate >= startDate && o.OrderDate <= endDate);
                var ordersProcessed = await ordersQuery.CountAsync();
                var salesAmount = await ordersQuery.Select(o => o.FinalAmount).DefaultIfEmpty(0m).SumAsync();
                var salesConversionRate = totalCalls == 0 ? 0m : Math.Round((decimal)ordersProcessed / totalCalls * 100m, 2);

                results.Add(new AgentPerformanceReportDto
                {
                    AgentId = agent.Id,
                    AgentName = agent.Name.Trim(),
                    TotalCallsHandled = totalCalls,
                    CompletedCalls = completedCalls,
                    AverageHandlingTime = TimeSpan.FromTicks((long)averageHandlingTicks),
                    TicketsResolved = resolvedTickets,
                    ResolutionRate = resolutionRate,
                    OrdersProcessed = ordersProcessed,
                    SalesAmount = salesAmount,
                    SalesConversionRate = salesConversionRate
                });
            }

            return results
                .OrderByDescending(r => r.TicketsResolved)
                .ThenByDescending(r => r.OrdersProcessed)
                .ToList();
        }

        private async Task<int?> GetCurrentUserBranchIdAsync()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }

        private static string BuildCsv(IEnumerable<AgentPerformanceReportDto> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("AgentId,AgentName,TotalCallsHandled,CompletedCalls,AverageHandlingTime,TicketsResolved,ResolutionRate,OrdersProcessed,SalesAmount,SalesConversionRate");

            foreach (var row in rows)
            {
                sb.Append(row.AgentId).Append(',')
                    .Append(EscapeCsv(row.AgentName)).Append(',')
                    .Append(row.TotalCallsHandled).Append(',')
                    .Append(row.CompletedCalls).Append(',')
                    .Append(EscapeCsv(row.AverageHandlingTime.ToString())).Append(',')
                    .Append(row.TicketsResolved).Append(',')
                    .Append(row.ResolutionRate).Append(',')
                    .Append(row.OrdersProcessed).Append(',')
                    .Append(row.SalesAmount).Append(',')
                    .Append(row.SalesConversionRate)
                    .AppendLine();
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        // Lightweight PDF generator to avoid external dependencies for report export.
        private static byte[] BuildSimplePdf(IEnumerable<AgentPerformanceReportDto> rows)
        {
            var lines = new List<string>
            {
                "ClassicFit Performance Report",
                $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                ""
            };

            lines.AddRange(rows.Select(r =>
                $"Agent: {r.AgentName} | Calls: {r.TotalCallsHandled} | Tickets Resolved: {r.TicketsResolved} | Orders: {r.OrdersProcessed} | Sales: {r.SalesAmount:0.00}"));

            var yStart = 780;
            var textBuilder = new StringBuilder();
            textBuilder.AppendLine("BT");
            textBuilder.AppendLine("/F1 11 Tf");
            foreach (var (line, index) in lines.Select((line, index) => (line, index)))
            {
                var y = yStart - (index * 16);
                var safeLine = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                textBuilder.AppendLine($"1 0 0 1 40 {y} Tm ({safeLine}) Tj");
            }
            textBuilder.AppendLine("ET");
            var content = textBuilder.ToString();

            var objects = new List<string>();
            objects.Add("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
            objects.Add("2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj");
            objects.Add("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj");
            objects.Add("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");
            objects.Add($"5 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}endstream endobj");

            var header = "%PDF-1.4\n";
            var bodyBuilder = new StringBuilder();
            var offsets = new List<int> { 0 };
            var currentLength = Encoding.ASCII.GetByteCount(header);

            foreach (var obj in objects)
            {
                offsets.Add(currentLength);
                bodyBuilder.Append(obj).Append('\n');
                currentLength += Encoding.ASCII.GetByteCount(obj) + 1;
            }

            var xrefStart = currentLength;
            var xrefBuilder = new StringBuilder();
            xrefBuilder.AppendLine($"xref\n0 {objects.Count + 1}");
            xrefBuilder.AppendLine("0000000000 65535 f ");
            for (var i = 1; i < offsets.Count; i++)
            {
                xrefBuilder.AppendLine($"{offsets[i]:D10} 00000 n ");
            }

            var trailer = $"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF";

            var final = header + bodyBuilder + xrefBuilder + trailer;
            return Encoding.ASCII.GetBytes(final);
        }
    }
}
