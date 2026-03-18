using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface ITicketService
    {
        Task<IEnumerable<TicketDto>> GetAllTicketsAsync();
        Task<TicketDto?> GetTicketByIdAsync(int id);
        Task<TicketDto> CreateTicketAsync(CreateTicketDto createTicketDto);
        Task<TicketDto?> UpdateTicketAsync(int id, UpdateTicketDto updateTicketDto);
        Task<bool> DeleteTicketAsync(int id);
        Task<TicketCommentDto> AddCommentAsync(CreateTicketCommentDto createCommentDto, int userId);
        Task<bool> AssignTicketAsync(int ticketId, int agentId);
        Task<bool> EscalateTicketAsync(int ticketId);
        Task<IEnumerable<TicketDto>> GetTicketsByAgentAsync(int agentId);
        Task<IEnumerable<TicketDto>> GetTicketsByCustomerAsync(int customerId);
        Task<int> BackfillUnassignedTicketsAsync();
    }
}
