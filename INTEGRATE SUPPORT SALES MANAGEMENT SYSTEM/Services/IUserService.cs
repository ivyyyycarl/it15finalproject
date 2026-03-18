using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public record ResetPasswordResult(bool Success, string Message);

    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto, string? createdByName);
        Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<ResetPasswordResult> ResetPasswordAsync(string email);
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task LogAuditActionAsync(string action, string description, int? userId = null, string? details = null);
        Task<IEnumerable<UserDto>> GetRecentlyActiveUsersAsync(int count = 10);
    }
}
