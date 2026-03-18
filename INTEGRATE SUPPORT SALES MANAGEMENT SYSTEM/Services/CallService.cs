using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class CallService : ICallService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallService> _logger;

        public CallService(ApplicationDbContext context, ILogger<CallService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CallDto>> GetAllCallsAsync()
        {
            var calls = await _context.Calls
                .Include(c => c.Agent)
                .Include(c => c.Customer)
                .OrderByDescending(c => c.StartTime)
                .ToListAsync();

            return calls.Select(c => new CallDto
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                Customer = c.Customer != null ? new CustomerDto
                {
                    Id = c.Customer.Id,
                    UserId = c.Customer.UserId,
                    FirstName = c.Customer.FirstName,
                    LastName = c.Customer.LastName,
                    Email = c.Customer.Email
                } : null,
                AgentId = c.AgentId,
                Agent = c.Agent != null ? new UserDto
                {
                    Id = c.Agent.Id,
                    BranchId = c.Agent.BranchId,
                    BranchName = c.Agent.Branch != null ? c.Agent.Branch.Name : null,
                    FirstName = c.Agent.FirstName,
                    LastName = c.Agent.LastName,
                    Email = c.Agent.Email
                } : null,
                Type = c.Type,
                Status = c.Status,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Duration = c.Duration,
                Subject = c.Subject,
                Notes = c.Notes,
                Outcome = c.Outcome,
                IsEscalated = c.IsEscalated,
                CreatedAt = c.CreatedAt
            });
        }

        public async Task<CallDto?> GetCallByIdAsync(int id)
        {
            var call = await _context.Calls
                .Include(c => c.Agent)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (call == null) return null;

            return new CallDto
            {
                Id = call.Id,
                CustomerId = call.CustomerId,
                Customer = call.Customer != null ? new CustomerDto
                {
                    Id = call.Customer.Id,
                    UserId = call.Customer.UserId,
                    FirstName = call.Customer.FirstName,
                    LastName = call.Customer.LastName,
                    Email = call.Customer.Email
                } : null,
                AgentId = call.AgentId,
                Agent = call.Agent != null ? new UserDto
                {
                    Id = call.Agent.Id,
                    BranchId = call.Agent.BranchId,
                    BranchName = call.Agent.Branch != null ? call.Agent.Branch.Name : null,
                    FirstName = call.Agent.FirstName,
                    LastName = call.Agent.LastName,
                    Email = call.Agent.Email
                } : null,
                Type = call.Type,
                Status = call.Status,
                StartTime = call.StartTime,
                EndTime = call.EndTime,
                Duration = call.Duration,
                Subject = call.Subject,
                Notes = call.Notes,
                Outcome = call.Outcome,
                IsEscalated = call.IsEscalated,
                CreatedAt = call.CreatedAt
            };
        }

        public async Task<CallDto> CreateCallAsync(CreateCallDto createCallDto)
        {
            var call = new Call
            {
                AgentId = createCallDto.AgentId,
                CustomerId = createCallDto.CustomerId,
                Subject = createCallDto.Subject,
                Notes = createCallDto.Notes,
                Type = createCallDto.Type,
                Status = CallStatus.Scheduled,
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Calls.Add(call);
            await _context.SaveChangesAsync();

            return (await GetCallByIdAsync(call.Id))!;
        }

        public async Task<CallDto?> UpdateCallAsync(int id, UpdateCallDto updateCallDto)
        {
            var call = await _context.Calls
                .FirstOrDefaultAsync(c => c.Id == id);

            if (call == null) return null;

            if (updateCallDto.Status.HasValue)
                call.Status = updateCallDto.Status.Value;

            if (updateCallDto.EndTime.HasValue)
                call.EndTime = updateCallDto.EndTime.Value;

            if (!string.IsNullOrEmpty(updateCallDto.Subject))
                call.Subject = updateCallDto.Subject;

            if (updateCallDto.Notes != null)
                call.Notes = updateCallDto.Notes;

            if (!string.IsNullOrEmpty(updateCallDto.Outcome))
                call.Outcome = updateCallDto.Outcome;

            if (updateCallDto.IsEscalated.HasValue)
                call.IsEscalated = updateCallDto.IsEscalated.Value;

            await _context.SaveChangesAsync();

            return await GetCallByIdAsync(id);
        }

        public Task<bool> DeleteCallAsync(int id)
        {
            return Task.FromResult(false);
        }

        public async Task<bool> StartCallAsync(int callId)
        {
            var call = await _context.Calls
                .FirstOrDefaultAsync(c => c.Id == callId);

            if (call == null) return false;

            call.Status = CallStatus.InProgress;
            call.StartTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EndCallAsync(int callId)
        {
            var call = await _context.Calls
                .FirstOrDefaultAsync(c => c.Id == callId);

            if (call == null) return false;

            call.Status = CallStatus.Completed;
            call.EndTime = DateTime.UtcNow;

            if (ShouldAutoCreateTicket(call))
            {
                await CreateAutoTicketForCallAsync(call);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CallSummaryDto> GetCallSummaryAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Calls.Where(c => c.AgentId == agentId);

            if (startDate.HasValue)
                query = query.Where(c => c.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.StartTime <= endDate.Value);

            var calls = await query.ToListAsync();

            var totalCalls = calls.Count;
            var completedCalls = calls.Count(c => c.Status == CallStatus.Completed);
            var missedCalls = calls.Count(c => c.Status == CallStatus.Missed);
            var escalatedCalls = calls.Count(c => c.IsEscalated);

            var durations = calls.Where(c => c.Duration.HasValue).Select(c => c.Duration!.Value).ToList();
            var averageDuration = durations.Any() ? TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)) : TimeSpan.Zero;

            return new CallSummaryDto
            {
                TotalCalls = totalCalls,
                CompletedCalls = completedCalls,
                MissedCalls = missedCalls,
                AverageDuration = averageDuration,
                EscalatedCalls = escalatedCalls
            };
        }

        public async Task<IEnumerable<CallDto>> GetCallsByAgentAsync(int agentId)
        {
            var calls = await _context.Calls
                .Include(c => c.Agent)
                .Include(c => c.Customer)
                .Where(c => c.AgentId == agentId)
                .OrderByDescending(c => c.StartTime)
                .ToListAsync();

            return calls.Select(c => new CallDto
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                Customer = c.Customer != null ? new CustomerDto
                {
                    Id = c.Customer.Id,
                    UserId = c.Customer.UserId,
                    FirstName = c.Customer.FirstName,
                    LastName = c.Customer.LastName,
                    Email = c.Customer.Email
                } : null,
                AgentId = c.AgentId,
                Agent = c.Agent != null ? new UserDto
                {
                    Id = c.Agent.Id,
                    BranchId = c.Agent.BranchId,
                    BranchName = c.Agent.Branch != null ? c.Agent.Branch.Name : null,
                    FirstName = c.Agent.FirstName,
                    LastName = c.Agent.LastName,
                    Email = c.Agent.Email
                } : null,
                Type = c.Type,
                Status = c.Status,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Duration = c.Duration,
                Subject = c.Subject,
                Notes = c.Notes,
                Outcome = c.Outcome,
                IsEscalated = c.IsEscalated,
                CreatedAt = c.CreatedAt
            });
        }

        public async Task<IEnumerable<CallDto>> GetCallsByCustomerAsync(int customerId)
        {
            var calls = await _context.Calls
                .Include(c => c.Agent)
                .Include(c => c.Customer)
                .Where(c => c.CustomerId == customerId)
                .OrderByDescending(c => c.StartTime)
                .ToListAsync();

            return calls.Select(c => new CallDto
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                Customer = c.Customer != null ? new CustomerDto
                {
                    Id = c.Customer.Id,
                    UserId = c.Customer.UserId,
                    FirstName = c.Customer.FirstName,
                    LastName = c.Customer.LastName,
                    Email = c.Customer.Email
                } : null,
                AgentId = c.AgentId,
                Agent = c.Agent != null ? new UserDto
                {
                    Id = c.Agent.Id,
                    BranchId = c.Agent.BranchId,
                    BranchName = c.Agent.Branch != null ? c.Agent.Branch.Name : null,
                    FirstName = c.Agent.FirstName,
                    LastName = c.Agent.LastName,
                    Email = c.Agent.Email
                } : null,
                Type = c.Type,
                Status = c.Status,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Duration = c.Duration,
                Subject = c.Subject,
                Notes = c.Notes,
                Outcome = c.Outcome,
                IsEscalated = c.IsEscalated,
                CreatedAt = c.CreatedAt
            });
        }

        private bool ShouldAutoCreateTicket(Call call)
        {
            if (call.IsEscalated)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(call.Outcome))
            {
                return true;
            }

            var outcome = call.Outcome.Trim().ToLowerInvariant();
            var unresolvedMarkers = new[]
            {
                "unresolved",
                "not resolved",
                "pending",
                "follow up",
                "callback",
                "escalated"
            };

            return unresolvedMarkers.Any(marker => outcome.Contains(marker));
        }

        private async Task CreateAutoTicketForCallAsync(Call call)
        {
            var alreadyExists = await _context.Tickets
                .AnyAsync(t => t.RelatedCallId == call.Id);

            if (alreadyExists)
            {
                return;
            }

            var ticket = new Ticket
            {
                TicketNumber = await GenerateTicketNumberAsync(),
                CustomerId = call.CustomerId,
                AssignedAgentId = call.AgentId,
                CreatedByUserId = call.AgentId,
                Title = $"Follow-up from {(call.Type == CallType.Inbound ? "inbound" : "customer")} interaction",
                Description = string.IsNullOrWhiteSpace(call.Notes)
                    ? "Auto-generated ticket due to unresolved interaction outcome."
                    : $"Auto-generated ticket due to unresolved interaction outcome. Notes: {call.Notes}",
                Priority = call.IsEscalated ? TicketPriority.High : TicketPriority.Medium,
                Category = TicketCategory.Service,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RelatedCallId = call.Id
            };

            _context.Tickets.Add(ticket);
            _logger.LogInformation("Auto-created ticket {TicketNumber} for unresolved call {CallId}", ticket.TicketNumber, call.Id);
        }

        private async Task<string> GenerateTicketNumberAsync()
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayCount = await _context.Tickets.CountAsync(t => t.TicketNumber.StartsWith($"TKT-{datePrefix}"));
            return $"TKT-{datePrefix}-{(todayCount + 1):D4}";
        }
    }
}
