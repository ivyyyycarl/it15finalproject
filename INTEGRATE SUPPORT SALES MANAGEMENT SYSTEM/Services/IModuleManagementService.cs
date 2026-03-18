using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IModuleManagementService
    {
        Task<ModuleAccessConfigDto> GetConfigurationAsync();
        Task<ModuleAccessConfigDto> UpdateConfigurationAsync(ModuleAccessConfigDto config);
    }
}
