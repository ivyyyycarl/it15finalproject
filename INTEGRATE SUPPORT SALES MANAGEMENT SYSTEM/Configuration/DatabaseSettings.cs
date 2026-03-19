namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration
{
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Provider { get; set; } = "SqlServer";
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = true;
        public int MaxRetryCount { get; set; } = 3;
    }
}
