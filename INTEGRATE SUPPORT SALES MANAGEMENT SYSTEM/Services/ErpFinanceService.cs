using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.EntityFrameworkCore;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class ErpFinanceService : IErpFinanceService
    {
        private readonly ApplicationDbContext _context;

        public ErpFinanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FinancialTransactionDto>> GetAllTransactionsAsync()
        {
            var transactions = await _context.FinancialTransactions
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            return transactions.Select(MapTransactionToDto).ToList();
        }

        public async Task<List<InvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = await _context.Invoices
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
            return invoices.Select(MapInvoiceToDto).ToList();
        }

        public async Task<List<PaymentDto>> GetAllPaymentsAsync()
        {
            var payments = await _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return payments.Select(MapPaymentToDto).ToList();
        }

        public async Task<InvoiceDto?> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                OrderId = createDto.OrderId ?? 0,
                CustomerId = createDto.CustomerId,
                SubtotalAmount = createDto.SubtotalAmount,
                TaxAmount = createDto.TaxAmount,
                DiscountAmount = createDto.DiscountAmount,
                TotalAmount = createDto.SubtotalAmount + createDto.TaxAmount - createDto.DiscountAmount,
                Status = InvoiceStatus.Sent,
                IssueDate = DateTime.UtcNow,
                DueDate = createDto.DueDate ?? DateTime.UtcNow.AddDays(30),
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return MapInvoiceToDto(invoice);
        }

        public async Task<PaymentDto?> RecordPaymentAsync(RecordPaymentDto recordDto)
        {
            var paymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var payment = new Payment
            {
                PaymentNumber = paymentNumber,
                InvoiceId = recordDto.InvoiceId ?? 0,
                Amount = recordDto.Amount,
                PaymentMethod = recordDto.PaymentMethod,
                TransactionReference = recordDto.TransactionReference,
                Status = PaymentStatus.Paid,
                PaymentDate = DateTime.UtcNow,
                Notes = recordDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            if (recordDto.InvoiceId.HasValue)
            {
                var invoice = await _context.Invoices.FindAsync(recordDto.InvoiceId.Value);
                if (invoice != null)
                {
                    var totalPaid = await _context.Payments
                        .Where(p => p.InvoiceId == recordDto.InvoiceId.Value && p.Status == PaymentStatus.Paid)
                        .SumAsync(p => p.Amount) + recordDto.Amount;

                    if (totalPaid >= invoice.TotalAmount)
                    {
                        invoice.Status = InvoiceStatus.Paid;
                        invoice.PaidDate = DateTime.UtcNow;
                    }
                    else
                    {
                        invoice.Status = InvoiceStatus.PartiallyPaid;
                    }
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return MapPaymentToDto(payment);
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync()
        {
            var totalRevenue = await _context.FinancialTransactions
                .Where(t => t.Type == TransactionType.Sale && t.Status == TransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            var totalExpenses = await _context.FinancialTransactions
                .Where(t => t.Type == TransactionType.Expense && t.Status == TransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            var invoices = await _context.Invoices.ToListAsync();

            var pendingInvoices = invoices.Where(i => i.Status == InvoiceStatus.Sent).ToList();
            var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
            var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

            return new FinancialSummaryDto
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
            };
        }

        public async Task<FinancialTransactionDto?> CreateTransactionAsync(CreateFinancialTransactionDto createDto)
        {
            var transactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var transaction = new FinancialTransaction
            {
                TransactionNumber = transactionNumber,
                Type = createDto.Type,
                Amount = createDto.Amount,
                Currency = createDto.Currency,
                TransactionDate = DateTime.UtcNow,
                OrderId = createDto.OrderId,
                PaymentId = createDto.PaymentId,
                Status = TransactionStatus.Completed,
                PaymentMethod = createDto.PaymentMethod,
                Description = createDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return MapTransactionToDto(transaction);
        }

        private static FinancialTransactionDto MapTransactionToDto(FinancialTransaction transaction)
        {
            return new FinancialTransactionDto
            {
                Id = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                TransactionDate = transaction.TransactionDate,
                OrderId = transaction.OrderId,
                PaymentId = transaction.PaymentId,
                Status = transaction.Status.ToString(),
                PaymentMethod = transaction.PaymentMethod,
                Description = transaction.Description
            };
        }

        private static InvoiceDto MapInvoiceToDto(Invoice invoice)
        {
            return new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                OrderId = invoice.OrderId,
                CustomerId = invoice.CustomerId,
                SubtotalAmount = invoice.SubtotalAmount,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status.ToString(),
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                PaidDate = invoice.PaidDate,
                Notes = invoice.Notes
            };
        }

        private static PaymentDto MapPaymentToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                PaymentNumber = payment.PaymentNumber,
                InvoiceId = payment.InvoiceId == 0 ? null : payment.InvoiceId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionReference = payment.TransactionReference,
                Status = payment.Status.ToString(),
                PaymentDate = payment.PaymentDate,
                Notes = payment.Notes
            };
        }
    }
}
