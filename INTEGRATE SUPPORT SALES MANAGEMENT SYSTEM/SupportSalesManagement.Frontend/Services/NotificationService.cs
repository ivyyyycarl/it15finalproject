using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public class NotificationService
    {
        private List<Notification> _notifications = new();
        private int _nextId = 1;

        public event Action? OnNotificationsChanged;

        public NotificationService()
        {
            _notifications = new List<Notification>();
        }

        public List<Notification> GetNotifications()
        {
            return _notifications.OrderByDescending(n => n.CreatedAt).ToList();
        }

        public List<Notification> GetUnreadNotifications()
        {
            return _notifications.Where(n => !n.IsRead).OrderByDescending(n => n.CreatedAt).ToList();
        }

        public int GetUnreadCount()
        {
            return _notifications.Count(n => !n.IsRead);
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                OnNotificationsChanged?.Invoke();
            }
        }

        public void MarkAllAsRead()
        {
            foreach (var notification in _notifications)
            {
                notification.IsRead = true;
            }
            OnNotificationsChanged?.Invoke();
        }

        public void AddNotification(string title, string message, NotificationType type)
        {
            _notifications.Add(new Notification
            {
                Id = _nextId++,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Icon = GetIconForType(type)
            });
            OnNotificationsChanged?.Invoke();
        }

        public void HandleDataChanged(string entityType, string action)
        {
            var (title, message, type) = entityType switch
            {
                "Ticket" => ($"Ticket {action}", $"A support ticket has been {action.ToLower()}", NotificationType.Info),
                "Order" => ($"Order {action}", $"An order has been {action.ToLower()}", NotificationType.Info),
                "Product" => ($"Product {action}", $"A product has been {action.ToLower()}", NotificationType.Info),
                "Invoice" => ($"Invoice {action}", $"An invoice has been {action.ToLower()}", NotificationType.Success),
                "Payment" => ($"Payment {action}", $"A payment has been {action.ToLower()}", NotificationType.Success),
                "Inventory" => ($"Inventory {action}", $"Inventory stock has been updated", NotificationType.Warning),
                _ => ($"Data {action}", $"{entityType} data has been {action.ToLower()}", NotificationType.Info)
            };

            AddNotification(title, message, type);
        }

        private string GetIconForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "check-circle",
                NotificationType.Warning => "alert-triangle",
                NotificationType.Error => "x-circle",
                _ => "info"
            };
        }
    }
}
