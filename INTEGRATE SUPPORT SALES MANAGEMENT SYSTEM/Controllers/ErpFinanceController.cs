using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using System.Security.Claims;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/erp/finance")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ErpFinanceController : ControllerBase
    {
        private readonly IErpFinanceService _financeService;
        private readonly IDataChangeNotifier _notifier;
        private readonly ApplicationDbContext _context;

        public ErpFinanceController(IErpFinanceService financeService, IDataChangeNotifier notifier, ApplicationDbContext context)
        {
            _financeService = financeService;
            _notifier = notifier;
            _context = context;
        }

        /// <summary>
        /// Get all financial transactions from ERP system
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _financeService.GetAllTransactionsAsync();
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Ok(new List<FinancialTransactionDto>());
                }

                var orderIds = await GetBranchOrderIdsAsync(branchId.Value);
                var paymentIds = await GetBranchPaymentIdsAsync(branchId.Value);
                transactions = transactions
                    .Where(t =>
                        (t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value)) ||
                        (t.PaymentId.HasValue && paymentIds.Contains(t.PaymentId.Value)))
                    .ToList();
            }

            return Ok(transactions);
        }

        /// <summary>
        /// Get all invoices from ERP system
        /// </summary>
        [HttpGet("invoices")]
        public async Task<IActionResult> GetAllInvoices()
        {
            var invoices = await _financeService.GetAllInvoicesAsync();
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Ok(new List<InvoiceDto>());
                }

                var orderIds = await GetBranchOrderIdsAsync(branchId.Value);
                var customerIds = await GetBranchCustomerIdsAsync(branchId.Value);
                invoices = invoices
                    .Where(i => orderIds.Contains(i.OrderId ?? 0) || customerIds.Contains(i.CustomerId))
                    .ToList();
            }

            return Ok(invoices);
        }

        /// <summary>
        /// Get all payments from ERP system
        /// </summary>
        [HttpGet("payments")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _financeService.GetAllPaymentsAsync();
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Ok(new List<PaymentDto>());
                }

                var invoiceIds = await GetBranchInvoiceIdsAsync(branchId.Value);
                payments = payments
                    .Where(p => p.InvoiceId.HasValue && invoiceIds.Contains(p.InvoiceId.Value))
                    .ToList();
            }

            return Ok(payments);
        }

        /// <summary>
        /// Create a new invoice in ERP system
        /// </summary>
        [HttpPost("invoices")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto createDto)
        {
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Forbid();
                }

                var canAccessCustomer = await CanAccessCustomerAsync(createDto.CustomerId, branchId.Value);
                var canAccessOrder = !createDto.OrderId.HasValue || await CanAccessOrderAsync(createDto.OrderId.Value, branchId.Value);
                if (!canAccessCustomer || !canAccessOrder)
                {
                    return Forbid();
                }
            }

            var invoice = await _financeService.CreateInvoiceAsync(createDto);
            if (invoice == null)
            {
                return UnprocessableEntity(new { message = "Failed to create invoice" });
            }

            await _notifier.NotifyDataChanged("Invoice", "Created");
            return CreatedAtAction(nameof(GetAllInvoices), new { id = invoice.Id }, invoice);
        }

        /// <summary>
        /// Record a payment in ERP system
        /// </summary>
        [HttpPost("payments")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto recordDto)
        {
            if (!IsSuperAdmin() && recordDto.InvoiceId.HasValue)
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Forbid();
                }

                var canAccessInvoice = await CanAccessInvoiceAsync(recordDto.InvoiceId.Value, branchId.Value);
                if (!canAccessInvoice)
                {
                    return Forbid();
                }
            }

            var payment = await _financeService.RecordPaymentAsync(recordDto);
            if (payment == null)
            {
                return UnprocessableEntity(new { message = "Failed to record payment" });
            }

            await _notifier.NotifyDataChanged("Payment", "Recorded");
            return CreatedAtAction(nameof(GetAllPayments), new { id = payment.Id }, payment);
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateFinancialTransactionDto createDto)
        {
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Forbid();
                }

                if (createDto.OrderId.HasValue)
                {
                    var canAccessOrder = await CanAccessOrderAsync(createDto.OrderId.Value, branchId.Value);
                    if (!canAccessOrder)
                    {
                        return Forbid();
                    }
                }

                if (createDto.PaymentId.HasValue)
                {
                    var canAccessPayment = await CanAccessPaymentAsync(createDto.PaymentId.Value, branchId.Value);
                    if (!canAccessPayment)
                    {
                        return Forbid();
                    }
                }
            }

            var transaction = await _financeService.CreateTransactionAsync(createDto);
            if (transaction == null)
            {
                return UnprocessableEntity(new { message = "Failed to create transaction" });
            }

            await _notifier.NotifyDataChanged("FinancialTransaction", "Created");
            return CreatedAtAction(nameof(GetAllTransactions), new { id = transaction.Id }, transaction);
        }

        /// <summary>
        /// Get financial summary from ERP system
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetFinancialSummary()
        {
            if (!IsSuperAdmin())
            {
                var branchId = await GetCurrentBranchIdAsync();
                if (!branchId.HasValue)
                {
                    return Ok(new FinancialSummaryDto { GeneratedAt = DateTime.UtcNow });
                }

                var orderIds = await GetBranchOrderIdsAsync(branchId.Value);
                var invoiceIds = await GetBranchInvoiceIdsAsync(branchId.Value);
                var paymentIds = await GetBranchPaymentIdsAsync(branchId.Value);

                var totalRevenue = await _context.FinancialTransactions
                    .AsNoTracking()
                    .Where(t => t.Type == TransactionType.Sale &&
                                t.Status == TransactionStatus.Completed &&
                                ((t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value)) ||
                                 (t.PaymentId.HasValue && paymentIds.Contains(t.PaymentId.Value))))
                    .SumAsync(t => t.Amount);

                var totalExpenses = await _context.FinancialTransactions
                    .AsNoTracking()
                    .Where(t => t.Type == TransactionType.Expense &&
                                t.Status == TransactionStatus.Completed &&
                                ((t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value)) ||
                                 (t.PaymentId.HasValue && paymentIds.Contains(t.PaymentId.Value))))
                    .SumAsync(t => t.Amount);

                var invoices = await _context.Invoices
                    .AsNoTracking()
                    .Where(i => invoiceIds.Contains(i.Id))
                    .ToListAsync();

                var pendingInvoices = invoices.Where(i => i.Status == InvoiceStatus.Sent).ToList();
                var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
                var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

                return Ok(new FinancialSummaryDto
                {
                    TotalRevenue = totalRevenue,
                    TotalExpenses = totalExpenses,
                    NetIncome = totalRevenue - totalExpenses,
                    PendingInvoicesAmount = pendingInvoices.Sum(i => i.TotalAmount),
                    PendingInvoicesCount = pendingInvoices.Count,
                    PaidInvoicesAmount = paidInvoices.Sum(i => i.TotalAmount),
                    PaidInvoicesCount = paidInvoices.Count,
                    OverdueInvoicesAmount = overdueInvoices.Sum(i => i.TotalAmount),
                    OverdueInvoicesCount = overdueInvoices.Count,
                    GeneratedAt = DateTime.UtcNow
                });
            }

            var summary = await _financeService.GetFinancialSummaryAsync();
            return Ok(summary);
        }

        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentBranchIdAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();
        }

        private async Task<HashSet<int>> GetBranchCustomerIdsAsync(int branchId)
        {
            var ids = await _context.Customers
                .AsNoTracking()
                .Where(c => c.User != null && c.User.BranchId == branchId)
                .Select(c => c.Id)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task<HashSet<int>> GetBranchOrderIdsAsync(int branchId)
        {
            var ids = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    (o.Agent != null && o.Agent.BranchId == branchId) ||
                    (o.Agent == null && o.Customer.User != null && o.Customer.User.BranchId == branchId))
                .Select(o => o.Id)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task<HashSet<int>> GetBranchInvoiceIdsAsync(int branchId)
        {
            var ids = await _context.Invoices
                .AsNoTracking()
                .Where(i =>
                    (i.Order != null && (
                        (i.Order.Agent != null && i.Order.Agent.BranchId == branchId) ||
                        (i.Order.Agent == null && i.Order.Customer.User != null && i.Order.Customer.User.BranchId == branchId))) ||
                    (i.Customer != null && i.Customer.User != null && i.Customer.User.BranchId == branchId))
                .Select(i => i.Id)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task<HashSet<int>> GetBranchPaymentIdsAsync(int branchId)
        {
            var ids = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Invoice != null && (
                    (p.Invoice.Order != null && (
                        (p.Invoice.Order.Agent != null && p.Invoice.Order.Agent.BranchId == branchId) ||
                        (p.Invoice.Order.Agent == null && p.Invoice.Order.Customer.User != null && p.Invoice.Order.Customer.User.BranchId == branchId))) ||
                    (p.Invoice.Customer != null && p.Invoice.Customer.User != null && p.Invoice.Customer.User.BranchId == branchId)))
                .Select(p => p.Id)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task<bool> CanAccessCustomerAsync(int customerId, int branchId)
        {
            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.Id == customerId && c.User != null && c.User.BranchId == branchId);
        }

        private async Task<bool> CanAccessOrderAsync(int orderId, int branchId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId &&
                    ((o.Agent != null && o.Agent.BranchId == branchId) ||
                     (o.Agent == null && o.Customer.User != null && o.Customer.User.BranchId == branchId)));
        }

        private async Task<bool> CanAccessInvoiceAsync(int invoiceId, int branchId)
        {
            return await _context.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.Id == invoiceId &&
                    ((i.Order != null && (
                        (i.Order.Agent != null && i.Order.Agent.BranchId == branchId) ||
                        (i.Order.Agent == null && i.Order.Customer.User != null && i.Order.Customer.User.BranchId == branchId))) ||
                     (i.Customer != null && i.Customer.User != null && i.Customer.User.BranchId == branchId)));
        }

        private async Task<bool> CanAccessPaymentAsync(int paymentId, int branchId)
        {
            return await _context.Payments
                .AsNoTracking()
                .AnyAsync(p => p.Id == paymentId && p.Invoice != null &&
                    ((p.Invoice.Order != null && (
                        (p.Invoice.Order.Agent != null && p.Invoice.Order.Agent.BranchId == branchId) ||
                        (p.Invoice.Order.Agent == null && p.Invoice.Order.Customer.User != null && p.Invoice.Order.Customer.User.BranchId == branchId))) ||
                     (p.Invoice.Customer != null && p.Invoice.Customer.User != null && p.Invoice.Customer.User.BranchId == branchId)));
        }
    }
}
