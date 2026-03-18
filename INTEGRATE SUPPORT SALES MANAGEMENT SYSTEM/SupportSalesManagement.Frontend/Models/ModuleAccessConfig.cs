namespace SupportSalesManagement.Frontend.Models
{
    public class ModuleAccessConfig
    {
        public List<ModuleAccessItem> Modules { get; set; } = new();
    }

    public class ModuleAccessItem
    {
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public bool IsEnabled { get; set; } = true;
        public RoleAccessMatrix RoleAccess { get; set; } = new();
    }

    public class RoleAccessMatrix
    {
        public bool SuperAdmin { get; set; } = true;
        public bool Admin { get; set; }
        public bool Supervisor { get; set; }
        public bool Agent { get; set; }
        public bool Customer { get; set; }
    }
}
