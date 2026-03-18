using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IErpFinanceService
    {
        Task<List<FinancialTransactionDto>> GetAllTransactionsAsync();
        Task<List<InvoiceDto>> GetAllInvoicesAsync();
        Task<List<PaymentDto>> GetAllPaymentsAsync();
        Task<InvoiceDto?> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<PaymentDto?> RecordPaymentAsync(RecordPaymentDto recordDto);
        Task<FinancialSummaryDto> GetFinancialSummaryAsync();
        Task<FinancialTransactionDto?> CreateTransactionAsync(CreateFinancialTransactionDto createDto);
    }
}
