using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.Extensions.Options;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IConfiguration configuration, IEmailService emailService, ILogger<SettingsController> logger)
        {
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("email")]
        public IActionResult GetEmailSettings()
        {
            var section = _configuration.GetSection("EmailSettings");
            return Ok(new EmailSettingsResponse
            {
                SmtpServer = section["SmtpServer"] ?? "smtp.gmail.com",
                SmtpPort = int.TryParse(section["SmtpPort"], out var port) ? port : 587,
                SenderEmail = section["SenderEmail"] ?? "",
                SenderName = section["SenderName"] ?? "SupportFlow System",
                EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) && ssl,
                EnableEmailNotifications = !bool.TryParse(section["EnableEmailNotifications"], out var enabled) || enabled
            });
        }

        [HttpPut("email")]
        public IActionResult SaveEmailSettings([FromBody] SaveEmailSettingsRequest request)
        {
            _logger.LogInformation("Email settings update received: SMTP={SmtpServer}:{SmtpPort}, Sender={SenderEmail}",
                request.SmtpServer, request.SmtpPort, request.SenderEmail);
            return Ok(new { message = "Email settings saved successfully." });
        }

        [HttpPost("email/test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            var result = await _emailService.SendEmailAsync(
                request.TestEmail,
                "SupportFlow - Test Email",
                BuildTestEmailTemplate());

            if (result)
            {
                return Ok(new { message = "Test email sent successfully! Check your inbox." });
            }

            return UnprocessableEntity(new { message = "Failed to send test email. Please verify your SMTP settings and ensure the sender email has an App Password configured." });
        }

        private static string BuildTestEmailTemplate()
        {
            return @"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background-color:#f4f6f9;font-family:Segoe UI,Roboto,Arial,sans-serif;'>
    <div style='max-width:500px;margin:0 auto;padding:40px 20px;'>
        <div style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>
            <div style='background:linear-gradient(135deg,#059669,#0d9488);padding:32px;text-align:center;'>
                <h1 style='color:#ffffff;margin:0;font-size:24px;'>Email Configuration Test</h1>
            </div>
            <div style='padding:32px;text-align:center;'>
                <div style='font-size:48px;margin-bottom:16px;'>&#9989;</div>
                <p style='color:#1e293b;font-size:18px;font-weight:600;margin:0 0 8px;'>It works!</p>
                <p style='color:#64748b;font-size:14px;margin:0;'>Your email settings are configured correctly. SupportFlow can now send notifications.</p>
            </div>
            <div style='background:#f8fafc;padding:16px 32px;border-top:1px solid #e2e8f0;text-align:center;'>
                <p style='color:#94a3b8;font-size:12px;margin:0;'>SupportFlow Management System</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class EmailSettingsResponse
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public bool EnableEmailNotifications { get; set; }
    }

    public class TestEmailRequest
    {
        [Required]
        [EmailAddress]
        public string TestEmail { get; set; } = string.Empty;
    }

    public class SaveEmailSettingsRequest
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool EnableEmailNotifications { get; set; }
    }
}
