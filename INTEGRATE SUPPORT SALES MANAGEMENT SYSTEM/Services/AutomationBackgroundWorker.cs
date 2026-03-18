using System;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class AutomationBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomationBackgroundWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public AutomationBackgroundWorker(IServiceProvider serviceProvider, ILogger<AutomationBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Background Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IErpInventoryService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                        await RunAutoEscalation(context, ticketService);
                        await RunLowStockAlerts(inventoryService);
                        await RunReportAggregation(context);
                        await RunOrderStatusProgression(context);
                        await RunInventorySync(inventoryService);
                        await RunPendingSubscriptionCleanup(context);
                        await RunSubscriptionDueReminders(context, emailService, configuration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Automation Background Worker.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task RunInventorySync(IErpInventoryService inventoryService)
        {
            var lowStockItems = await inventoryService.GetLowStockItemsAsync(null);
            if (lowStockItems.Any())
            {
                _logger.LogInformation("Low stock alert: {Count} items below reorder level", lowStockItems.Count);
            }
        }

        private async Task RunOrderStatusProgression(ApplicationDbContext context)
        {
            _logger.LogInformation("Processing Automated Order Status Progression...");

            // Advance Pending to Shipped (after 2 hours)
            var pendingOrders = await context.Orders
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < DateTime.UtcNow.AddHours(-2))
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                order.Status = OrderStatus.Shipped;
                order.ShippingDate = DateTime.UtcNow;
                _logger.LogInformation($"AUTOMATION: Order {order.OrderNumber} status updated to Shipped.");
            }

            // Advance Shipped to Delivered (after 24 hours)
            var shippedOrders = await context.Orders
                .Where(o => o.Status == OrderStatus.Shipped && o.ShippingDate < DateTime.UtcNow.AddHours(-24))
                .ToListAsync();

            foreach (var order in shippedOrders)
            {
                order.Status = OrderStatus.Delivered;
                order.DeliveryDate = DateTime.UtcNow;
                _logger.LogInformation($"AUTOMATION: Order {order.OrderNumber} status updated to Delivered.");
            }

            await context.SaveChangesAsync();
        }

        private async Task RunAutoEscalation(ApplicationDbContext context, ITicketService ticketService)
        {
            _logger.LogInformation("Running Ticket Auto-Escalation Check...");
            var inactivityThreshold = DateTime.UtcNow.AddHours(-24);

            var staleTickets = await context.Tickets
                .Where(t => t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Priority != TicketPriority.Critical &&
                            t.UpdatedAt < inactivityThreshold)
                .ToListAsync();

            foreach (var ticket in staleTickets)
            {
                await ticketService.EscalateTicketAsync(ticket.Id);
                _logger.LogWarning($"AUTOMATION: Ticket {ticket.TicketNumber} auto-escalated due to 24h inactivity.");
            }
        }

        private async Task RunLowStockAlerts(IErpInventoryService inventoryService)
        {
            _logger.LogInformation("Running Low Stock Alerts Check...");
            var lowStockItems = await inventoryService.GetLowStockItemsAsync(null);

            foreach (var item in lowStockItems)
            {
                _logger.LogWarning($"AUTOMATION ALERT: Low Stock for SKU {item.SKU} ({item.ProductName}). Current: {item.StockQuantity}, Reorder Level: {item.ReorderLevel}");
            }
        }

        private async Task RunReportAggregation(ApplicationDbContext context)
        {
            _logger.LogInformation("Running Daily Report Aggregation...");
            // Simplified logic for simulation
            var today = DateTime.UtcNow.Date;
            if (!await context.PerformanceReports.AnyAsync(r => r.ReportDate.Date == today))
            {
                var agents = await context.Users.Where(u => u.Role == UserRole.Agent).ToListAsync();
                foreach (var agent in agents)
                {
                    var resolvedCount = await context.Tickets.CountAsync(t => t.AssignedAgentId == agent.Id && t.ResolvedAt >= today);
                    var calls = await context.Calls.Where(c => c.AgentId == agent.Id && c.StartTime >= today).ToListAsync();
                    var totalDuration = TimeSpan.FromMinutes(calls.Sum(c => c.Duration?.TotalMinutes ?? 0));

                    var report = new PerformanceReport
                    {
                        AgentId = agent.Id,
                        TicketsResolved = resolvedCount,
                        TotalCallsHandled = calls.Count,
                        TotalCallDuration = totalDuration,
                        AvgHandlingTime = calls.Count > 0 ? TimeSpan.FromTicks(totalDuration.Ticks / calls.Count) : TimeSpan.Zero,
                        ResolutionRate = calls.Count > 0 ? (decimal)resolvedCount / calls.Count : 0,
                        SalesConversionRate = 0,
                        ReportDate = today
                    };
                    context.PerformanceReports.Add(report);
                }
                await context.SaveChangesAsync();
                _logger.LogInformation($"AUTOMATION: Daily performance reports generated for {today:yyyy-MM-dd}.");
            }
        }

        private async Task RunPendingSubscriptionCleanup(ApplicationDbContext context)
        {
            _logger.LogInformation("Running stale pending subscription cleanup...");

            var staleThreshold = DateTime.UtcNow.AddHours(-24);
            var stalePending = await context.PendingSubscriptionOnboardings
                .Where(p => !p.IsCompleted && p.CreatedAt <= staleThreshold)
                .ToListAsync();

            if (!stalePending.Any())
            {
                return;
            }

            var staleUserIds = stalePending.Select(p => p.AdminUserId).Distinct().ToList();
            var inactiveStaleUsers = await context.Users
                .Where(u => staleUserIds.Contains(u.Id) && !u.IsActive && u.Role == UserRole.Admin)
                .ToListAsync();

            context.PendingSubscriptionOnboardings.RemoveRange(stalePending);
            context.Users.RemoveRange(inactiveStaleUsers);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Removed {PendingCount} stale pending subscriptions and {UserCount} inactive pre-created users.",
                stalePending.Count,
                inactiveStaleUsers.Count);

            // Keep completed onboarding history lean (90 days retention).
            var completedRetentionCutoff = DateTime.UtcNow.AddDays(-90);
            var oldCompleted = await context.PendingSubscriptionOnboardings
                .Where(p => p.IsCompleted && p.CompletedAt != null && p.CompletedAt <= completedRetentionCutoff)
                .ToListAsync();

            if (oldCompleted.Any())
            {
                context.PendingSubscriptionOnboardings.RemoveRange(oldCompleted);
                await context.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} old completed onboarding records.", oldCompleted.Count);
            }
        }

        private async Task RunSubscriptionDueReminders(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            var today = DateTime.UtcNow.Date;
            var reminderOffsets = new HashSet<int> { 7, 3, 1, 0 };

            var subscriptions = await context.TenantSubscriptions
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .Where(s =>
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.PastDue) &&
                    s.NextBillingAt.HasValue)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                return;
            }

            var recipients = await context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Role == UserRole.Admin)
                .Select(u => new { u.Email, u.FirstName })
                .ToListAsync();

            if (!recipients.Any())
            {
                _logger.LogInformation("Subscription reminder skipped: no active Admin recipients.");
                return;
            }

            var appBaseUrl = configuration["App:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(appBaseUrl))
            {
                appBaseUrl = "http://localhost:5250";
            }

            foreach (var subscription in subscriptions)
            {
                var dueDate = subscription.NextBillingAt!.Value.Date;
                var daysUntilDue = (dueDate - today).Days;

                if (!reminderOffsets.Contains(daysUntilDue))
                {
                    continue;
                }

                var planName = subscription.SubscriptionPlan?.Name ?? "Current Plan";
                foreach (var recipient in recipients)
                {
                    if (string.IsNullOrWhiteSpace(recipient.Email))
                    {
                        continue;
                    }

                    var dedupeKey = $"sub:{subscription.Id}|email:{recipient.Email.Trim().ToLowerInvariant()}|days:{daysUntilDue}";
                    var sentToday = await context.AuditLogs
                        .AsNoTracking()
                        .AnyAsync(a =>
                            a.Action == "Subscription Payment Reminder Sent" &&
                            a.Details == dedupeKey &&
                            a.Timestamp >= today &&
                            a.Timestamp < today.AddDays(1));

                    if (sentToday)
                    {
                        continue;
                    }

                    var subject = daysUntilDue == 0
                        ? $"Payment due today - {subscription.TenantName} subscription"
                        : $"Subscription payment reminder ({daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")} left)";

                    var body = BuildSubscriptionReminderHtml(
                        firstName: recipient.FirstName,
                        tenantName: subscription.TenantName,
                        planName: planName,
                        dueDate: dueDate,
                        daysUntilDue: daysUntilDue,
                        manageUrl: $"{appBaseUrl}/subscription/overview");

                    var sent = await emailService.SendEmailAsync(recipient.Email, subject, body);
                    if (!sent)
                    {
                        _logger.LogWarning(
                            "Failed to send subscription reminder to {Email} for tenant {Tenant}.",
                            recipient.Email,
                            subscription.TenantName);
                        continue;
                    }

                    context.AuditLogs.Add(new AuditLog
                    {
                        Action = "Subscription Payment Reminder Sent",
                        Description = $"Payment reminder sent to {recipient.Email} for tenant '{subscription.TenantName}'.",
                        UserEmail = "system@automation.local",
                        Timestamp = DateTime.UtcNow,
                        Details = dedupeKey
                    });

                    _logger.LogInformation(
                        "Subscription reminder sent to {Email} for tenant {Tenant} ({DaysUntilDue} day(s) left).",
                        recipient.Email,
                        subscription.TenantName,
                        daysUntilDue);
                }
            }

            await context.SaveChangesAsync();
        }

        private static string BuildSubscriptionReminderHtml(
            string? firstName,
            string tenantName,
            string planName,
            DateTime dueDate,
            int daysUntilDue,
            string manageUrl)
        {
            var greetingName = string.IsNullOrWhiteSpace(firstName) ? "Admin" : firstName.Trim();
            var dueLabel = daysUntilDue switch
            {
                0 => "today",
                1 => "tomorrow",
                _ => $"in {daysUntilDue} days"
            };

            return $@"
<div style='font-family:Segoe UI,Arial,sans-serif;color:#1f2937;line-height:1.6'>
  <h2 style='margin:0 0 12px;color:#1d4ed8'>Subscription Payment Reminder</h2>
  <p>Hello {greetingName},</p>
  <p>Your company subscription payment is due <strong>{dueLabel}</strong>.</p>
  <ul>
    <li><strong>Company:</strong> {tenantName}</li>
    <li><strong>Plan:</strong> {planName}</li>
    <li><strong>Due date:</strong> {dueDate:MMMM dd, yyyy}</li>
  </ul>
  <p>Please settle payment on or before the due date to avoid service interruption.</p>
  <p>
    <a href='{manageUrl}' style='display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:10px 16px;border-radius:8px;font-weight:600'>
      Manage Subscription
    </a>
  </p>
  <p style='font-size:12px;color:#6b7280'>This is an automated reminder from ClassicFit Pro.</p>
</div>";
        }
    }
}
