using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketService> _logger;
        private readonly IEmailService _emailService;

        public TicketService(ApplicationDbContext context, ILogger<TicketService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IEnumerable<TicketDto>> GetAllTicketsAsync()
        {
            var tickets = await _context.Tickets
                .Include(t => t.AssignedAgent)
                .Include(t => t.Customer)
                .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tickets.Select(t => new TicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                CustomerId = t.CustomerId,
                Customer = t.Customer != null ? new CustomerDto
                {
                    Id = t.Customer.Id,
                    FirstName = t.Customer.FirstName,
                    LastName = t.Customer.LastName,
                    Email = t.Customer.Email
                } : null,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgent = t.AssignedAgent != null ? new UserDto
                {
                    Id = t.AssignedAgent.Id,
                    FirstName = t.AssignedAgent.FirstName,
                    LastName = t.AssignedAgent.LastName,
                    Email = t.AssignedAgent.Email
                } : null,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                ResolvedAt = t.ResolvedAt,
                Resolution = t.Resolution,
                RelatedCallId = t.RelatedCallId,
                Comments = t.Comments.Select(c => new TicketCommentDto
                {
                    Id = c.Id,
                    TicketId = c.TicketId,
                    UserId = c.UserId,
                    User = c.User != null ? new UserDto
                    {
                        Id = c.User.Id,
                        FirstName = c.User.FirstName,
                        LastName = c.User.LastName,
                        Email = c.User.Email
                    } : null,
                    Comment = c.Comment,
                    IsInternal = c.IsInternal,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList()
            });
        }

        public async Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.AssignedAgent)
                .Include(t => t.Customer)
                .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return null;

            return new TicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                CustomerId = ticket.CustomerId,
                Customer = ticket.Customer != null ? new CustomerDto
                {
                    Id = ticket.Customer.Id,
                    FirstName = ticket.Customer.FirstName,
                    LastName = ticket.Customer.LastName,
                    Email = ticket.Customer.Email
                } : null,
                AssignedAgentId = ticket.AssignedAgentId,
                AssignedAgent = ticket.AssignedAgent != null ? new UserDto
                {
                    Id = ticket.AssignedAgent.Id,
                    FirstName = ticket.AssignedAgent.FirstName,
                    LastName = ticket.AssignedAgent.LastName,
                    Email = ticket.AssignedAgent.Email
                } : null,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                Category = ticket.Category,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                Resolution = ticket.Resolution,
                RelatedCallId = ticket.RelatedCallId,
                Comments = ticket.Comments.Select(c => new TicketCommentDto
                {
                    Id = c.Id,
                    TicketId = c.TicketId,
                    UserId = c.UserId,
                    User = c.User != null ? new UserDto
                    {
                        Id = c.User.Id,
                        FirstName = c.User.FirstName,
                        LastName = c.User.LastName,
                        Email = c.User.Email
                    } : null,
                    Comment = c.Comment,
                    IsInternal = c.IsInternal,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList()
            };
        }

        public async Task<TicketDto> CreateTicketAsync(CreateTicketDto createTicketDto)
        {
            var ticketNumber = await GenerateTicketNumberAsync();
            var assignedAgentId = createTicketDto.AssignedAgentId;

            // Auto-assign customer-created tickets when no explicit agent is provided.
            // Choose the active agent with the lowest open-ticket workload.
            if (!assignedAgentId.HasValue)
            {
                assignedAgentId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive && u.Role == UserRole.Agent)
                    .Select(u => new
                    {
                        u.Id,
                        OpenTicketCount = _context.Tickets.Count(t =>
                            t.AssignedAgentId == u.Id &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Closed)
                    })
                    .OrderBy(x => x.OpenTicketCount)
                    .ThenBy(x => x.Id)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();
            }

            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                CustomerId = createTicketDto.CustomerId,
                AssignedAgentId = assignedAgentId,
                CreatedByUserId = createTicketDto.CreatedByUserId,
                Title = createTicketDto.Title,
                Description = createTicketDto.Description,
                Status = TicketStatus.Open,
                Priority = createTicketDto.Priority,
                Category = createTicketDto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Send ticket created email to customer
            await SendTicketCreatedEmailAsync(ticket);

            return (await GetTicketByIdAsync(ticket.Id))!;
        }

        public async Task<TicketDto?> UpdateTicketAsync(int id, UpdateTicketDto updateTicketDto)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return null;

            if (!string.IsNullOrEmpty(updateTicketDto.Title))
                ticket.Title = updateTicketDto.Title;

            if (updateTicketDto.Description != null)
                ticket.Description = updateTicketDto.Description;

            string? oldStatus = null;
            if (updateTicketDto.Status.HasValue)
            {
                oldStatus = ticket.Status.ToString();
                ticket.Status = updateTicketDto.Status.Value;
                if (updateTicketDto.Status.Value == TicketStatus.Resolved)
                    ticket.ResolvedAt = DateTime.UtcNow;
            }

            if (updateTicketDto.Priority.HasValue)
                ticket.Priority = updateTicketDto.Priority.Value;

            if (updateTicketDto.Category.HasValue)
                ticket.Category = updateTicketDto.Category.Value;

            if (updateTicketDto.AssignedAgentId.HasValue)
                ticket.AssignedAgentId = updateTicketDto.AssignedAgentId.Value;

            if (updateTicketDto.Resolution != null)
                ticket.Resolution = updateTicketDto.Resolution;

            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send email notification for status changes
            if (oldStatus != null)
            {
                await SendTicketStatusEmailAsync(ticket, oldStatus, updateTicketDto.Resolution);
            }

            return await GetTicketByIdAsync(id);
        }

        public async Task<bool> DeleteTicketAsync(int id)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return false;

            ticket.Status = TicketStatus.Closed;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }



        public async Task<IEnumerable<TicketDto>> GetTicketsByAgentAsync(int agentId)
        {
            var tickets = await _context.Tickets
                .Include(t => t.AssignedAgent)
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Where(t => t.AssignedAgentId == agentId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tickets.Select(t => new TicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}" : string.Empty,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? $"{t.AssignedAgent.FirstName} {t.AssignedAgent.LastName}" : string.Empty,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                ResolvedAt = t.ResolvedAt,
                CommentCount = t.Comments.Count
            });
        }

        public async Task<IEnumerable<TicketDto>> GetTicketsByCustomerAsync(int customerId)
        {
            var tickets = await _context.Tickets
                .Include(t => t.AssignedAgent)
                .Include(t => t.Customer)
                .Include(t => t.Comments)
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tickets.Select(t => new TicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}" : string.Empty,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? $"{t.AssignedAgent.FirstName} {t.AssignedAgent.LastName}" : string.Empty,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Category = t.Category,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                ResolvedAt = t.ResolvedAt,
                CommentCount = t.Comments.Count
            });
        }

        public async Task<int> BackfillUnassignedTicketsAsync()
        {
            var assignableTickets = await _context.Tickets
                .Where(t => !t.AssignedAgentId.HasValue &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Closed)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            if (!assignableTickets.Any())
            {
                return 0;
            }

            var agentLoads = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Role == UserRole.Agent)
                .Select(u => new AgentLoad
                {
                    AgentId = u.Id,
                    OpenTicketCount = _context.Tickets.Count(t =>
                        t.AssignedAgentId == u.Id &&
                        t.Status != TicketStatus.Resolved &&
                        t.Status != TicketStatus.Closed)
                })
                .OrderBy(x => x.OpenTicketCount)
                .ThenBy(x => x.AgentId)
                .ToListAsync();

            if (!agentLoads.Any())
            {
                return 0;
            }

            foreach (var ticket in assignableTickets)
            {
                var targetAgent = agentLoads
                    .OrderBy(a => a.OpenTicketCount)
                    .ThenBy(a => a.AgentId)
                    .First();

                ticket.AssignedAgentId = targetAgent.AgentId;
                ticket.UpdatedAt = DateTime.UtcNow;
                targetAgent.OpenTicketCount++;
            }

            await _context.SaveChangesAsync();
            return assignableTickets.Count;
        }

        private async Task<string> GenerateTicketNumberAsync()
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayCount = await _context.Tickets.CountAsync(t => t.TicketNumber.StartsWith($"TKT-{datePrefix}"));
            return $"TKT-{datePrefix}-{(todayCount + 1):D4}";
        }

        private sealed class AgentLoad
        {
            public int AgentId { get; set; }
            public int OpenTicketCount { get; set; }
        }

        public async Task<bool> AssignTicketAsync(int ticketId, int agentId)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return false;

            ticket.AssignedAgentId = agentId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EscalateTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return false;

            ticket.Priority = TicketPriority.Critical;
            ticket.Status = TicketStatus.InProgress;
            ticket.UpdatedAt = DateTime.UtcNow;

            // Automation: Log escalation event
            _logger.LogWarning($"AUTOMATION: Ticket {ticket.TicketNumber} has been AUTO-ESCALATED due to priority/inactivity.");

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task SendTicketCreatedEmailAsync(Ticket ticket)
        {
            try
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == ticket.CustomerId);
                if (customer == null) return;

                await _emailService.SendTicketCreatedEmailAsync(new TicketCreatedEmailData
                {
                    CustomerEmail = customer.Email,
                    CustomerFirstName = customer.FirstName,
                    TicketNumber = ticket.TicketNumber,
                    TicketTitle = ticket.Title,
                    Priority = ticket.Priority.ToString(),
                    Category = ticket.Category.ToString(),
                    CreatedAt = ticket.CreatedAt
                });

                await LogTicketEmailAuditAsync(ticket.TicketNumber, customer.Email, "Ticket created notification sent");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ticket created email for {TicketNumber}", ticket.TicketNumber);
            }
        }

        private async Task SendTicketStatusEmailAsync(Ticket ticket, string oldStatus, string? resolution)
        {
            try
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == ticket.CustomerId);
                if (customer == null) return;

                await _emailService.SendTicketStatusUpdateEmailAsync(new TicketStatusEmailData
                {
                    CustomerEmail = customer.Email,
                    CustomerFirstName = customer.FirstName,
                    TicketNumber = ticket.TicketNumber,
                    TicketTitle = ticket.Title,
                    OldStatus = oldStatus,
                    NewStatus = ticket.Status.ToString(),
                    Resolution = resolution ?? ticket.Resolution,
                    UpdatedAt = DateTime.UtcNow
                });

                await LogTicketEmailAuditAsync(ticket.TicketNumber, customer.Email, $"Ticket status changed: {oldStatus} -> {ticket.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ticket status email for {TicketNumber}", ticket.TicketNumber);
            }
        }

        private async Task LogTicketEmailAuditAsync(string ticketNumber, string customerEmail, string description)
        {
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Email Notification",
                    Description = $"[Ticket {ticketNumber}] {description}",
                    UserEmail = customerEmail,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Ticket: {ticketNumber}, Recipient: {customerEmail}"
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log ticket email audit for {TicketNumber}", ticketNumber);
            }
        }

        public async Task<TicketCommentDto> AddCommentAsync(CreateTicketCommentDto createCommentDto, int userId)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == createCommentDto.TicketId);

            if (ticket == null)
                throw new InvalidOperationException("Ticket not found");

            var comment = new TicketComment
            {
                TicketId = createCommentDto.TicketId,
                UserId = userId,
                Comment = createCommentDto.Comment,
                IsInternal = createCommentDto.IsInternal,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketComments.Add(comment);
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var createdComment = await _context.TicketComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            if (createdComment == null)
                throw new InvalidOperationException("Failed to retrieve created comment");

            return new TicketCommentDto
            {
                Id = createdComment.Id,
                TicketId = createdComment.TicketId,
                UserId = createdComment.UserId,
                User = createdComment.User != null ? new UserDto
                {
                    Id = createdComment.User.Id,
                    FirstName = createdComment.User.FirstName,
                    LastName = createdComment.User.LastName,
                    Email = createdComment.User.Email
                } : null,
                AuthorName = createdComment.User != null ? $"{createdComment.User.FirstName} {createdComment.User.LastName}" : string.Empty,
                Comment = createdComment.Comment,
                IsInternal = createdComment.IsInternal,
                CreatedAt = createdComment.CreatedAt,
                UpdatedAt = createdComment.UpdatedAt
            };
        }
    }
}
