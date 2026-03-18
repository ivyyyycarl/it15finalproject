using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IEmailService
    {
        // Account emails
        Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName, string role, string tempPassword);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string tempPassword);
        Task<bool> SendAccountCreatedByAdminEmailAsync(string toEmail, string firstName, string lastName, string role, string tempPassword, string createdByName);
        Task<bool> SendCompanySubscriptionActivatedEmailAsync(string toEmail, string firstName, string companyName, string planName, DateTime nextBillingAt, string loginUrl);

        // Purchase / Order emails
        Task<bool> SendPurchaseConfirmationEmailAsync(PurchaseConfirmationData data);
        Task<bool> SendOrderStatusUpdateEmailAsync(OrderStatusUpdateData data);
        Task<bool> SendShipmentTrackingEmailAsync(ShipmentTrackingData data);

        // Ticket emails
        Task<bool> SendTicketStatusUpdateEmailAsync(TicketStatusEmailData data);
        Task<bool> SendTicketCreatedEmailAsync(TicketCreatedEmailData data);

        // Generic
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    public class PurchaseConfirmationData
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string CustomerLastName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public List<PurchaseItemData> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMethod { get; set; } = "N/A";
        public string PaymentStatus { get; set; } = "Pending";
        public string? ShippingAddress { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public bool ErpSynced { get; set; }
    }

    public class PurchaseItemData
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderStatusUpdateData
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ShipmentTrackingData
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime ShippingDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? ShippingAddress { get; set; }
    }

    public class TicketStatusEmailData
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string TicketTitle { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TicketCreatedEmailData
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string TicketTitle { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
