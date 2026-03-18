using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface ICallService
    {
        Task<IEnumerable<CallDto>> GetAllCallsAsync();
        Task<IEnumerable<CallDto>> GetCallsByAgentAsync(int agentId);
        Task<IEnumerable<CallDto>> GetCallsByCustomerAsync(int customerId);
        Task<CallDto?> GetCallByIdAsync(int id);
        Task<CallDto> CreateCallAsync(CreateCallDto createCallDto);
        Task<CallDto?> UpdateCallAsync(int id, UpdateCallDto updateCallDto);
        Task<bool> DeleteCallAsync(int id);
        Task<bool> StartCallAsync(int callId);
        Task<bool> EndCallAsync(int callId);
        Task<CallSummaryDto> GetCallSummaryAsync(int agentId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
