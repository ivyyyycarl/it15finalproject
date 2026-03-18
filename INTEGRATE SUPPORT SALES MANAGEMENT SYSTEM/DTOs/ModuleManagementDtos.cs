namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs
{
    public class ModuleAccessConfigDto
    {
        public List<ModuleAccessItemDto> Modules { get; set; } = new();
    }

    public class ModuleAccessItemDto
    {
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public bool IsEnabled { get; set; } = true;
        public RoleAccessDto RoleAccess { get; set; } = new();
    }

    public class RoleAccessDto
    {
        public bool SuperAdmin { get; set; } = true;
        public bool Admin { get; set; }
        public bool Supervisor { get; set; }
        public bool Agent { get; set; }
        public bool Customer { get; set; }
    }
}
