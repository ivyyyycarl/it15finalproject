using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Call> Calls { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<TicketComment> TicketComments { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<PerformanceReport> PerformanceReports { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<TenantSubscription> TenantSubscriptions { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<PendingSubscriptionOnboarding> PendingSubscriptionOnboardings { get; set; } = null!;
        public DbSet<ModuleDefinition> ModuleDefinitions { get; set; } = null!;
        public DbSet<PlanModuleEntitlement> PlanModuleEntitlements { get; set; } = null!;
        public DbSet<TenantModuleOverride> TenantModuleOverrides { get; set; } = null!;
        public DbSet<UsageEvent> UsageEvents { get; set; } = null!;
        public DbSet<UsagePeriodSummary> UsagePeriodSummaries { get; set; } = null!;
        public DbSet<SubscriptionInvoiceRecord> SubscriptionInvoiceRecords { get; set; } = null!;
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(e => e.Branch)
                    .WithMany(b => b.Users)
                    .HasForeignKey(e => e.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.State).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(c => c.User)
                    .WithOne()
                    .HasForeignKey<Customer>(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Call configuration
            modelBuilder.Entity<Call>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Subject).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                entity.Property(e => e.Outcome).HasMaxLength(100);
                entity.Property(e => e.StartTime).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Agent)
                    .WithMany(e => e.Calls)
                    .HasForeignKey(e => e.AgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Calls)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ticket configuration
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Resolution).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.TicketNumber).IsUnique();

                entity.HasOne(e => e.AssignedAgent)
                    .WithMany(e => e.AssignedTickets)
                    .HasForeignKey(e => e.AssignedAgentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CreatedByAgent)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Tickets)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RelatedCall)
                    .WithMany(e => e.CreatedTickets)
                    .HasForeignKey(e => e.RelatedCallId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // TicketComment configuration
            modelBuilder.Entity<TicketComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Ticket)
                    .WithMany(e => e.Comments)
                    .HasForeignKey(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FinalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.PaymentIntentId).HasMaxLength(100);

                entity.HasIndex(e => e.OrderNumber).IsUnique();

                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Agent)
                    .WithMany(e => e.Orders)
                    .HasForeignKey(e => e.AgentId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.RelatedCall)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedCallId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // OrderDetail configuration
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Order)
                    .WithMany(e => e.OrderDetails)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderDetails)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.Branch)
                    .WithMany(b => b.Products)
                    .HasForeignKey(e => e.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.BranchId, e.SKU }).IsUnique();
            });

            // PerformanceReport configuration
            modelBuilder.Entity<PerformanceReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SalesConversionRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ResolutionRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ReportDate).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Agent)
                    .WithMany(e => e.PerformanceReports)
                    .HasForeignKey(e => e.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Details).HasMaxLength(4000);

                entity.HasIndex(e => e.Timestamp);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Invoice configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.IssueDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.InvoiceNumber).IsUnique();

                entity.HasOne(e => e.Order)
                    .WithMany(e => e.Invoices)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Invoices)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaymentNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransactionReference).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.PaymentDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.PaymentNumber).IsUnique();

                entity.HasOne(e => e.Invoice)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FinancialTransaction configuration
            modelBuilder.Entity<FinancialTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.TransactionNumber).IsUnique();

                entity.HasOne(e => e.Order)
                    .WithMany(e => e.FinancialTransactions)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Payment)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AnnualPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SoftLimitGracePercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.IncludedModulesCsv).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<TenantSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.BillingCycle).HasMaxLength(20);
                entity.Property(e => e.StripeCustomerId).HasMaxLength(120);
                entity.Property(e => e.StripeSubscriptionId).HasMaxLength(120);
                entity.Property(e => e.Currency).HasMaxLength(25);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LastPaymentStatus).HasMaxLength(60);
                entity.Property(e => e.CancelReason).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.TenantName);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                entity.Property(e => e.AddressLine).HasMaxLength(250);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Province).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.ZipCode).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<PendingSubscriptionOnboarding>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.AdminEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AdminFirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AdminLastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.InitialBranchName).HasMaxLength(150);
                entity.Property(e => e.CheckoutSessionId).HasMaxLength(255);
                entity.Property(e => e.StripeCustomerId).HasMaxLength(120);
                entity.Property(e => e.CheckoutStatus).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.CheckoutSessionId).IsUnique();
                entity.HasIndex(e => e.IsCompleted);
                entity.HasIndex(e => e.AdminEmail);
            });

            modelBuilder.Entity<ModuleDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModuleKey).IsRequired().HasMaxLength(80);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(80);
                entity.Property(e => e.AllowAdmin).HasDefaultValue(true);
                entity.Property(e => e.AllowSupervisor).HasDefaultValue(true);
                entity.Property(e => e.AllowAgent).HasDefaultValue(true);
                entity.Property(e => e.AllowCustomer).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.ModuleKey).IsUnique();
            });

            modelBuilder.Entity<PlanModuleEntitlement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ModuleDefinition)
                    .WithMany(m => m.PlanEntitlements)
                    .HasForeignKey(e => e.ModuleDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.SubscriptionPlanId, e.ModuleDefinitionId }).IsUnique();
            });

            modelBuilder.Entity<TenantModuleOverride>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.ModuleKey).IsRequired().HasMaxLength(80);
                entity.Property(e => e.Reason).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.TenantName, e.ModuleKey }).IsUnique();
            });

            modelBuilder.Entity<UsageEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Dimension).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.SourceType).HasMaxLength(120);
                entity.Property(e => e.OccurredAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.TenantName, e.Dimension, e.OccurredAt });
            });

            modelBuilder.Entity<UsagePeriodSummary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Dimension).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UsedQuantity).HasColumnType("decimal(18,4)");
                entity.Property(e => e.AllowedQuantity).HasColumnType("decimal(18,4)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.TenantName, e.Dimension, e.PeriodStart, e.PeriodEnd }).IsUnique();
            });

            modelBuilder.Entity<SubscriptionInvoiceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StripeInvoiceId).HasMaxLength(120);
                entity.Property(e => e.StripePaymentIntentId).HasMaxLength(120);
                entity.Property(e => e.AmountDue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.TenantSubscription)
                    .WithMany()
                    .HasForeignKey(e => e.TenantSubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.StripeInvoiceId);
                entity.HasIndex(e => e.TenantSubscriptionId);
            });

            modelBuilder.Entity<RefundRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.OrderId, e.Status });
                entity.HasIndex(e => e.CreatedAt);
            });

            // Performance indexes
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.Status);
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.CustomerId);
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.AssignedAgentId);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.AgentId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.PaymentStatus);
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.Status, o.OrderDate });
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.PaymentStatus, o.OrderDate });
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.CustomerId, o.OrderDate });
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.AgentId, o.OrderDate });

            modelBuilder.Entity<Call>()
                .HasIndex(c => c.AgentId);
            modelBuilder.Entity<Call>()
                .HasIndex(c => c.CustomerId);

            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.Role, u.IsActive });
            modelBuilder.Entity<User>()
                .HasIndex(u => u.LastLoginAt);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.BranchId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.StockQuantity);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.BranchId);
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.IsActive, p.Category });
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.StockQuantity, p.MinStockLevel });

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.UserId);
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CreatedByUserId);
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.LastName);
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Company);

        }
    }
}
