using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/module-access")]
    public class ModuleAccessController : ControllerBase
    {
        private readonly IModuleManagementService _moduleManagementService;

        public ModuleAccessController(IModuleManagementService moduleManagementService)
        {
            _moduleManagementService = moduleManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<ModuleAccessConfigDto>> GetModuleAccess()
        {
            var config = await _moduleManagementService.GetConfigurationAsync();
            return Ok(config);
        }
    }
}
