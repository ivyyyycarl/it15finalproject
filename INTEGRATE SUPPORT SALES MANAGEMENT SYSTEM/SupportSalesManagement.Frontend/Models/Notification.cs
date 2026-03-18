namespace SupportSalesManagement.Frontend.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
