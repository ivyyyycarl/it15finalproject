namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "SupportFlow System";
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool EnableEmailNotifications { get; set; } = true;
    }
}
