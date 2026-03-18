using System.Net;
using System.Net.Mail;
using System.Text;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.Extensions.Options;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        // ClassicFit Pro Brand Colors
        private const string SteelBlue = "#4682B4";
        private const string SteelBlueDark = "#3A6F9A";
        private const string SoftTeal = "#5F9EA0";
        private const string MintGreen = "#A8D8B9";
        private const string MintGreenLight = "#E8F5EC";
        private const string WarmCream = "#FFF8DC";
        private const string WarmCreamDark = "#F5EDCF";

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        // ===================== ACCOUNT EMAILS =====================

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName, string lastName, string role, string tempPassword)
        {
            var subject = "Welcome to ClassicFit Pro - Your Account Has Been Created";
            var body = BuildWelcomeEmailTemplate(firstName, lastName, role, toEmail, tempPassword);
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string tempPassword)
        {
            var subject = "ClassicFit Pro - Your Password Has Been Reset";
            var body = BuildPasswordResetTemplate(firstName, toEmail, tempPassword);
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendAccountCreatedByAdminEmailAsync(string toEmail, string firstName, string lastName, string role, string tempPassword, string createdByName)
        {
            var subject = "Welcome to ClassicFit Pro - Your Account Is Ready";
            var body = BuildAccountCreatedByAdminTemplate(firstName, lastName, role, toEmail, tempPassword, createdByName);
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendCompanySubscriptionActivatedEmailAsync(string toEmail, string firstName, string companyName, string planName, DateTime nextBillingAt, string loginUrl)
        {
            var subject = $"ClassicFit Pro - Subscription Activated for {companyName}";
            var body = BuildCompanySubscriptionActivatedTemplate(firstName, companyName, planName, nextBillingAt, loginUrl);
            return await SendEmailAsync(toEmail, subject, body);
        }

        // ===================== PURCHASE / ORDER EMAILS =====================

        public async Task<bool> SendPurchaseConfirmationEmailAsync(PurchaseConfirmationData data)
        {
            var subject = "Your ClassicFit Purchase Confirmation";
            var body = BuildPurchaseConfirmationTemplate(data);
            return await SendEmailAsync(data.CustomerEmail, subject, body);
        }

        public async Task<bool> SendOrderStatusUpdateEmailAsync(OrderStatusUpdateData data)
        {
            var subject = $"ClassicFit Pro - Order {data.OrderNumber} Status Update";
            var body = BuildOrderStatusUpdateTemplate(data);
            return await SendEmailAsync(data.CustomerEmail, subject, body);
        }

        public async Task<bool> SendShipmentTrackingEmailAsync(ShipmentTrackingData data)
        {
            var subject = $"ClassicFit Pro - Your Order {data.OrderNumber} Has Been Shipped!";
            var body = BuildShipmentTrackingTemplate(data);
            return await SendEmailAsync(data.CustomerEmail, subject, body);
        }

        // ===================== TICKET EMAILS =====================

        public async Task<bool> SendTicketStatusUpdateEmailAsync(TicketStatusEmailData data)
        {
            var subject = $"ClassicFit Pro - Ticket {data.TicketNumber} Status Update";
            var body = BuildTicketStatusUpdateTemplate(data);
            return await SendEmailAsync(data.CustomerEmail, subject, body);
        }

        public async Task<bool> SendTicketCreatedEmailAsync(TicketCreatedEmailData data)
        {
            var subject = $"ClassicFit Pro - Support Ticket {data.TicketNumber} Created";
            var body = BuildTicketCreatedTemplate(data);
            return await SendEmailAsync(data.CustomerEmail, subject, body);
        }

        // ===================== SEND ENGINE =====================

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (!_settings.EnableEmailNotifications)
            {
                _logger.LogInformation("Email notifications disabled. Skipping email to {Email}", toEmail);
                return true;
            }

            if (string.IsNullOrEmpty(_settings.SenderEmail) || string.IsNullOrEmpty(_settings.Password))
            {
                _logger.LogWarning("Email settings not configured. Email to {Email} not sent.", toEmail);
                return false;
            }

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort);
                client.Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password);
                client.EnableSsl = _settings.EnableSsl;
                client.Timeout = 30000;

                await client.SendMailAsync(message);
                _logger.LogInformation("[EMAIL SENT] To: {Email} | Subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "[EMAIL FAILED] SMTP error to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL FAILED] Error to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        // ===================== SHARED TEMPLATE PARTS =====================

        private static string EmailWrapper(string headerBg, string headerTitle, string headerSubtitle, string bodyContent)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background-color:#f0f4f8;font-family:Segoe UI,Roboto,Helvetica Neue,Arial,sans-serif;'>
    <div style='max-width:640px;margin:0 auto;padding:32px 16px;'>
        <div style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.06);'>
            <!-- Header -->
            <div style='background:{headerBg};padding:36px 32px;text-align:center;'>
                <p style='color:rgba(255,255,255,0.85);font-size:13px;letter-spacing:1.5px;text-transform:uppercase;margin:0 0 8px;font-weight:600;'>ClassicFit Pro</p>
                <h1 style='color:#ffffff;margin:0;font-size:26px;font-weight:700;line-height:1.3;'>{headerTitle}</h1>
                {(string.IsNullOrEmpty(headerSubtitle) ? "" : $"<p style='color:rgba(255,255,255,0.75);margin:10px 0 0;font-size:15px;'>{headerSubtitle}</p>")}
            </div>
            <!-- Body -->
            <div style='padding:32px;'>
                {bodyContent}
            </div>
            <!-- Footer -->
            <div style='background:#f8fafc;padding:24px 32px;border-top:1px solid #e2e8f0;'>
                <table style='width:100%;'>
                    <tr>
                        <td style='text-align:center;'>
                            <p style='color:#94a3b8;font-size:12px;margin:0 0 8px;'>
                                ClassicFit Pro | Support Sales Management System
                            </p>
                            <p style='color:#94a3b8;font-size:11px;margin:0 0 8px;'>
                                Need help? Contact us at <a href='mailto:support@classicfitpro.com' style='color:{SteelBlue};text-decoration:none;'>support@classicfitpro.com</a>
                            </p>
                            <p style='color:#cbd5e1;font-size:10px;margin:0;'>
                                <a href='#' style='color:#cbd5e1;text-decoration:underline;'>Terms &amp; Conditions</a> &nbsp;|&nbsp;
                                <a href='#' style='color:#cbd5e1;text-decoration:underline;'>Privacy Policy</a>
                            </p>
                            <p style='color:#cbd5e1;font-size:10px;margin:8px 0 0;'>
                                This is an automated message. Please do not reply directly to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private static string CtaButton(string text, string bgColor)
        {
            return $@"<div style='text-align:center;margin:28px 0;'>
                <a href='#' style='display:inline-block;background:{bgColor};color:#ffffff;font-size:15px;font-weight:600;padding:14px 36px;border-radius:8px;text-decoration:none;box-shadow:0 2px 8px rgba(0,0,0,0.12);'>{text}</a>
            </div>";
        }

        private static string InfoBox(string bgColor, string borderColor, string textColor, string iconHtml, string content)
        {
            return $@"<div style='background:{bgColor};border:1px solid {borderColor};border-radius:8px;padding:14px 16px;margin:16px 0;display:flex;'>
                <span style='margin-right:10px;font-size:16px;'>{iconHtml}</span>
                <p style='color:{textColor};font-size:13px;margin:0;line-height:1.5;'>{content}</p>
            </div>";
        }

        // ===================== PURCHASE CONFIRMATION TEMPLATE =====================

        private static string BuildPurchaseConfirmationTemplate(PurchaseConfirmationData data)
        {
            var itemsHtml = new StringBuilder();
            foreach (var item in data.Items)
            {
                itemsHtml.Append($@"
                    <tr>
                        <td style='padding:12px 8px;border-bottom:1px solid #f1f5f9;'>
                            <p style='color:#1e293b;font-size:14px;font-weight:600;margin:0;'>{item.ProductName}</p>
                            <p style='color:#94a3b8;font-size:11px;margin:2px 0 0;'>SKU: {item.SKU}</p>
                        </td>
                        <td style='padding:12px 8px;border-bottom:1px solid #f1f5f9;text-align:center;color:#475569;font-size:14px;'>{item.Quantity}</td>
                        <td style='padding:12px 8px;border-bottom:1px solid #f1f5f9;text-align:right;color:#475569;font-size:14px;'>&#8369;{item.UnitPrice:N2}</td>
                        <td style='padding:12px 8px;border-bottom:1px solid #f1f5f9;text-align:right;color:#1e293b;font-size:14px;font-weight:600;'>&#8369;{item.TotalPrice:N2}</td>
                    </tr>");
            }

            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{data.CustomerFirstName} {data.CustomerLastName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Thank you for your purchase! Your order has been received and is being processed. Below are the details of your transaction.
                </p>

                <!-- Order Summary Header -->
                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:4px 0;'>
                                <span style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Order Number</span><br/>
                                <span style='color:#1e293b;font-size:16px;font-weight:700;'>{data.OrderNumber}</span>
                            </td>
                            <td style='padding:4px 0;text-align:right;'>
                                <span style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Purchase Date</span><br/>
                                <span style='color:#1e293b;font-size:14px;font-weight:600;'>{data.OrderDate:MMMM dd, yyyy hh:mm tt}</span>
                            </td>
                        </tr>
                        <tr>
                            <td style='padding:8px 0 0;'>
                                <span style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Transaction ID</span><br/>
                                <span style='color:#1e293b;font-size:13px;font-family:monospace;'>{data.TransactionId}</span>
                            </td>
                            <td style='padding:8px 0 0;text-align:right;'>
                                <span style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Payment</span><br/>
                                <span style='display:inline-block;background:{SteelBlue};color:#fff;font-size:12px;font-weight:600;padding:3px 10px;border-radius:12px;'>{data.PaymentStatus}</span>
                            </td>
                        </tr>
                    </table>
                </div>

                <!-- Items Table -->
                <p style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 8px;'>Items Purchased</p>
                <div style='border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;margin:0 0 20px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <thead>
                            <tr style='background:#f8fafc;'>
                                <th style='padding:10px 8px;text-align:left;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Product</th>
                                <th style='padding:10px 8px;text-align:center;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Qty</th>
                                <th style='padding:10px 8px;text-align:right;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Price</th>
                                <th style='padding:10px 8px;text-align:right;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                    </table>
                </div>

                <!-- Totals -->
                <div style='border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;margin:0 0 24px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:10px 16px;color:#64748b;font-size:14px;'>Subtotal</td>
                            <td style='padding:10px 16px;text-align:right;color:#1e293b;font-size:14px;'>&#8369;{data.Subtotal:N2}</td>
                        </tr>
                        <tr>
                            <td style='padding:10px 16px;color:#64748b;font-size:14px;border-top:1px solid #f1f5f9;'>Tax</td>
                            <td style='padding:10px 16px;text-align:right;color:#1e293b;font-size:14px;border-top:1px solid #f1f5f9;'>&#8369;{data.TaxAmount:N2}</td>
                        </tr>
                        {(data.DiscountAmount > 0 ? $@"<tr>
                            <td style='padding:10px 16px;color:#059669;font-size:14px;border-top:1px solid #f1f5f9;'>Discount</td>
                            <td style='padding:10px 16px;text-align:right;color:#059669;font-size:14px;border-top:1px solid #f1f5f9;'>-&#8369;{data.DiscountAmount:N2}</td>
                        </tr>" : "")}
                        <tr style='background:{MintGreenLight};'>
                            <td style='padding:14px 16px;color:#1e293b;font-size:16px;font-weight:700;border-top:2px solid {MintGreen};'>Total Paid</td>
                            <td style='padding:14px 16px;text-align:right;color:#1e293b;font-size:18px;font-weight:700;border-top:2px solid {MintGreen};'>&#8369;{data.FinalAmount:N2}</td>
                        </tr>
                    </table>
                </div>

                <!-- Payment & Shipping Details -->
                <div style='display:flex;gap:16px;margin:0 0 20px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='width:50%;vertical-align:top;padding-right:8px;'>
                                <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;'>
                                    <p style='color:#64748b;font-size:11px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 8px;'>Payment Method</p>
                                    <p style='color:#1e293b;font-size:14px;font-weight:600;margin:0;'>{data.PaymentMethod}</p>
                                </div>
                            </td>
                            <td style='width:50%;vertical-align:top;padding-left:8px;'>
                                <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;'>
                                    <p style='color:#64748b;font-size:11px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 8px;'>Shipping Address</p>
                                    <p style='color:#1e293b;font-size:13px;margin:0;line-height:1.5;'>{data.ShippingAddress ?? "To be confirmed"}</p>
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>

                {(data.EstimatedDeliveryDate.HasValue ? $@"
                <div style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:8px;padding:14px 16px;margin:0 0 16px;'>
                    <p style='color:#1d4ed8;font-size:13px;margin:0;'>
                        <strong>Estimated Delivery:</strong> {data.EstimatedDeliveryDate.Value:MMMM dd, yyyy}
                    </p>
                </div>" : "")}

                {(data.ErpSynced ? InfoBox(MintGreenLight, MintGreen, "#065f46", "&#9989;", "<strong>ERP Synced:</strong> Your order has been synced with our enterprise resource planning system for fulfillment.") : "")}

                {CtaButton("View Your Order", SteelBlue)}

                <p style='color:#94a3b8;font-size:12px;text-align:center;margin:0;'>
                    If you have any questions about your order, please contact our support team with your order number <strong>{data.OrderNumber}</strong>.
                </p>";

            return EmailWrapper(
                $"linear-gradient(135deg,{SteelBlue},{SoftTeal})",
                "Purchase Confirmation",
                "Thank you for your order!",
                bodyContent);
        }

        // ===================== ORDER STATUS UPDATE TEMPLATE =====================

        private static string BuildOrderStatusUpdateTemplate(OrderStatusUpdateData data)
        {
            var statusColor = data.NewStatus switch
            {
                OrderStatus.Processing => "#f59e0b",
                OrderStatus.Shipped => "#3b82f6",
                OrderStatus.Delivered => "#059669",
                OrderStatus.Cancelled => "#dc2626",
                OrderStatus.Refunded => "#7c3aed",
                _ => SteelBlue
            };

            var statusIcon = data.NewStatus switch
            {
                OrderStatus.Processing => "&#9881;",
                OrderStatus.Shipped => "&#128666;",
                OrderStatus.Delivered => "&#9989;",
                OrderStatus.Cancelled => "&#10060;",
                OrderStatus.Refunded => "&#128176;",
                _ => "&#128230;"
            };

            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{data.CustomerFirstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    There's an update on your order. Here are the details:
                </p>

                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;text-align:center;'>
                    <p style='font-size:36px;margin:0 0 8px;'>{statusIcon}</p>
                    <p style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 4px;'>Order {data.OrderNumber}</p>
                    <p style='color:#1e293b;font-size:13px;margin:0 0 12px;font-family:monospace;'>Transaction ID: {data.TransactionId}</p>
                    <div style='display:inline-block;background:{statusColor};color:#fff;font-size:14px;font-weight:700;padding:6px 20px;border-radius:20px;text-transform:uppercase;letter-spacing:0.5px;'>
                        {data.NewStatus}
                    </div>
                    <p style='color:#94a3b8;font-size:12px;margin:8px 0 0;'>{data.UpdatedAt:MMMM dd, yyyy hh:mm tt}</p>
                </div>

                <table style='width:100%;border-collapse:collapse;background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;margin:0 0 24px;'>
                    <tr>
                        <td style='padding:12px 16px;color:#64748b;font-size:13px;border-bottom:1px solid #e2e8f0;'>Previous Status</td>
                        <td style='padding:12px 16px;color:#1e293b;font-size:13px;font-weight:600;border-bottom:1px solid #e2e8f0;text-align:right;'>{data.OldStatus}</td>
                    </tr>
                    <tr>
                        <td style='padding:12px 16px;color:#64748b;font-size:13px;'>New Status</td>
                        <td style='padding:12px 16px;color:{statusColor};font-size:13px;font-weight:700;text-align:right;'>{data.NewStatus}</td>
                    </tr>
                </table>

                {(string.IsNullOrEmpty(data.Notes) ? "" : InfoBox("#f8fafc", "#e2e8f0", "#475569", "&#128221;", $"<strong>Note:</strong> {data.Notes}"))}

                {CtaButton("View Your Order", SteelBlue)}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SteelBlue},{SoftTeal})",
                "Order Status Update",
                $"Order {data.OrderNumber}",
                bodyContent);
        }

        // ===================== SHIPMENT TRACKING TEMPLATE =====================

        private static string BuildShipmentTrackingTemplate(ShipmentTrackingData data)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{data.CustomerFirstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Great news! Your order <strong>{data.OrderNumber}</strong> has been shipped and is on its way to you.
                </p>

                <div style='background:{MintGreenLight};border:1px solid {MintGreen};border-radius:10px;padding:24px;margin:0 0 24px;text-align:center;'>
                    <p style='font-size:40px;margin:0 0 12px;'>&#128666;</p>
                    <p style='color:#065f46;font-size:16px;font-weight:700;margin:0 0 4px;'>Your Order is On Its Way!</p>
                    <p style='color:#047857;font-size:13px;margin:0;'>Shipped on {data.ShippingDate:MMMM dd, yyyy}</p>
                    {(data.EstimatedDeliveryDate.HasValue ? $"<p style='color:#047857;font-size:13px;margin:6px 0 0;'><strong>Estimated Delivery:</strong> {data.EstimatedDeliveryDate.Value:MMMM dd, yyyy}</p>" : "")}
                </div>

                {(string.IsNullOrEmpty(data.ShippingAddress) ? "" : $@"
                <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:0 0 24px;'>
                    <p style='color:#64748b;font-size:11px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 8px;'>Shipping To</p>
                    <p style='color:#1e293b;font-size:14px;margin:0;line-height:1.5;'>{data.ShippingAddress}</p>
                </div>")}

                {CtaButton("Track Your Shipment", SoftTeal)}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SoftTeal},#0d9488)",
                "Your Order Has Shipped!",
                $"Order {data.OrderNumber}",
                bodyContent);
        }

        // ===================== TICKET STATUS UPDATE TEMPLATE =====================

        private static string BuildTicketStatusUpdateTemplate(TicketStatusEmailData data)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{data.CustomerFirstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    There's an update on your support ticket. Our team is working to resolve your issue as quickly as possible.
                </p>

                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Ticket</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;font-weight:700;text-align:right;'>{data.TicketNumber}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Subject</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;text-align:right;'>{data.TicketTitle}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Previous Status</td>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;text-align:right;'>{data.OldStatus}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>New Status</td>
                            <td style='padding:6px 0;color:{SteelBlue};font-size:14px;font-weight:700;text-align:right;'>{data.NewStatus}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Updated</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;text-align:right;'>{data.UpdatedAt:MMMM dd, yyyy hh:mm tt}</td>
                        </tr>
                    </table>
                </div>

                {(string.IsNullOrEmpty(data.Resolution) ? "" : $@"
                <div style='background:{MintGreenLight};border:1px solid {MintGreen};border-radius:8px;padding:16px;margin:0 0 20px;'>
                    <p style='color:#064e3b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;margin:0 0 6px;'>Resolution</p>
                    <p style='color:#065f46;font-size:14px;margin:0;line-height:1.5;'>{data.Resolution}</p>
                </div>")}

                {CtaButton("View Ticket", SteelBlue)}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SteelBlue},{SteelBlueDark})",
                "Ticket Status Update",
                $"Ticket {data.TicketNumber}",
                bodyContent);
        }

        // ===================== TICKET CREATED TEMPLATE =====================

        private static string BuildTicketCreatedTemplate(TicketCreatedEmailData data)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{data.CustomerFirstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Your support ticket has been created successfully. Our team will review it and get back to you as soon as possible.
                </p>

                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;font-weight:600;'>Ticket Number</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:15px;font-weight:700;text-align:right;'>{data.TicketNumber}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Subject</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;text-align:right;'>{data.TicketTitle}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Priority</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;text-align:right;'>{data.Priority}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Category</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;text-align:right;'>{data.Category}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:12px;'>Created</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;text-align:right;'>{data.CreatedAt:MMMM dd, yyyy hh:mm tt}</td>
                        </tr>
                    </table>
                </div>

                {InfoBox("#eff6ff", "#bfdbfe", "#1d4ed8", "&#128161;", "You can track your ticket status anytime from your customer dashboard. Our team typically responds within 24 hours.")}

                {CtaButton("View Ticket", SteelBlue)}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SoftTeal},{SteelBlue})",
                "Support Ticket Created",
                $"We've received your request",
                bodyContent);
        }

        // ===================== ACCOUNT EMAIL TEMPLATES =====================

        private static string BuildWelcomeEmailTemplate(string firstName, string lastName, string role, string email, string tempPassword)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{firstName} {lastName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Welcome! Your <strong>{role}</strong> account on ClassicFit Pro has been created. You can now log in using the credentials below.
                </p>
                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <p style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;margin:0 0 10px;font-weight:600;'>Your Login Credentials</p>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;width:100px;'>Email:</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;'>{email}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Password:</td>
                            <td style='padding:6px 0;'><span style='color:#1e293b;font-size:13px;font-weight:600;font-family:monospace;background:#fef3c7;padding:3px 8px;border-radius:4px;'>{tempPassword}</span></td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Role:</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;'>{role}</td>
                        </tr>
                    </table>
                </div>
                {InfoBox("#fef2f2", "#fecaca", "#dc2626", "&#9888;&#65039;", "<strong>Important:</strong> Please change your password after your first login for security.")}
                {CtaButton("Log In Now", SteelBlue)}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SteelBlue},{SoftTeal})",
                "Welcome to ClassicFit Pro",
                "Your account has been created",
                bodyContent);
        }

        private static string BuildPasswordResetTemplate(string firstName, string email, string tempPassword)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{firstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Your password has been reset. Use the temporary password below to log in, then change it immediately.
                </p>
                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <p style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;margin:0 0 10px;font-weight:600;'>Temporary Login Credentials</p>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;width:120px;'>Email:</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;'>{email}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Temp Password:</td>
                            <td style='padding:6px 0;'><span style='color:#1e293b;font-size:13px;font-weight:600;font-family:monospace;background:#fef3c7;padding:3px 8px;border-radius:4px;'>{tempPassword}</span></td>
                        </tr>
                    </table>
                </div>
                {InfoBox("#fef2f2", "#fecaca", "#dc2626", "&#128274;", "<strong>Security Notice:</strong> Change your password immediately after logging in. If you did not request this reset, contact your administrator.")}
                {CtaButton("Log In Now", SteelBlue)}";

            return EmailWrapper(
                "linear-gradient(135deg,#dc2626,#ea580c)",
                "Password Reset",
                "Your password has been reset",
                bodyContent);
        }

        private static string BuildAccountCreatedByAdminTemplate(string firstName, string lastName, string role, string email, string tempPassword, string createdByName)
        {
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{firstName} {lastName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    <strong>{createdByName}</strong> has created a <strong>{role}</strong> account for you on ClassicFit Pro. You can now log in using the credentials below.
                </p>
                <div style='background:{WarmCream};border:1px solid {WarmCreamDark};border-radius:10px;padding:20px;margin:0 0 24px;'>
                    <p style='color:#64748b;font-size:12px;text-transform:uppercase;letter-spacing:0.5px;margin:0 0 10px;font-weight:600;'>Your Login Credentials</p>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;width:100px;'>Email:</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;'>{email}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Password:</td>
                            <td style='padding:6px 0;'><span style='color:#1e293b;font-size:13px;font-weight:600;font-family:monospace;background:#fef3c7;padding:3px 8px;border-radius:4px;'>{tempPassword}</span></td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Role:</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:13px;font-weight:600;'>{role}</td>
                        </tr>
                    </table>
                </div>
                {InfoBox("#fef2f2", "#fecaca", "#dc2626", "&#9888;&#65039;", "<strong>Important:</strong> Please change your password after your first login for security.")}
                {InfoBox("#eff6ff", "#bfdbfe", "#1d4ed8", "&#128161;", "<strong>Getting Started:</strong> Log in and navigate to your dashboard to explore the features available for your role.")}
                {CtaButton("Log In Now", SteelBlue)}";

            return EmailWrapper(
                $"linear-gradient(135deg,#059669,{SoftTeal})",
                "Account Created",
                "An administrator has set up your account",
                bodyContent);
        }

        private static string BuildCompanySubscriptionActivatedTemplate(string firstName, string companyName, string planName, DateTime nextBillingAt, string loginUrl)
        {
            var safeLoginUrl = string.IsNullOrWhiteSpace(loginUrl) ? "#" : loginUrl;
            var bodyContent = $@"
                <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 8px;'>
                    Hi <strong>{firstName}</strong>,
                </p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 20px;'>
                    Your company subscription has been activated successfully and your workspace is now ready.
                </p>

                <div style='background:{MintGreenLight};border:1px solid {MintGreen};border-radius:10px;padding:18px;margin:0 0 20px;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;width:140px;'>Company</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;font-weight:600;'>{companyName}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Plan</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;font-weight:600;'>{planName}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 0;color:#64748b;font-size:13px;'>Next Billing Date</td>
                            <td style='padding:6px 0;color:#1e293b;font-size:14px;font-weight:600;'>{nextBillingAt:MMMM dd, yyyy}</td>
                        </tr>
                    </table>
                </div>

                <div style='text-align:center;margin:28px 0;'>
                    <a href='{safeLoginUrl}' style='display:inline-block;background:{SteelBlue};color:#ffffff;font-size:15px;font-weight:600;padding:14px 36px;border-radius:8px;text-decoration:none;box-shadow:0 2px 8px rgba(0,0,0,0.12);'>Go to Login</a>
                </div>

                {InfoBox("#eff6ff", "#bfdbfe", "#1d4ed8", "&#128161;", "You can now log in as your admin user and begin setting up branches, users, and modules.")}";

            return EmailWrapper(
                $"linear-gradient(135deg,{SteelBlue},{SoftTeal})",
                "Subscription Activated",
                "Welcome to your new workspace",
                bodyContent);
        }
    }
}
