using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
        Task<CustomerDto?> GetCustomerByIdAsync(int id);
        Task<CustomerDto?> GetCustomerByEmailAsync(string email);
        Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createCustomerDto);
        Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerDto updateCustomerDto);
        Task<bool> DeleteCustomerAsync(int id);
        Task<CustomerInteractionDto> GetCustomerInteractionHistoryAsync(int customerId);
        Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm);
        Task<CustomerDto?> GetCustomerByUserIdAsync(int userId);
        Task<bool> LinkCustomerToUserAsync(int customerId, int userId);
    }
}
