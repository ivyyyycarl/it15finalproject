using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    public class UploadFileRequest
    {
        public IFormFile? File { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;
        private readonly IEntitlementService _entitlementService;

        public UploadController(
            IWebHostEnvironment environment,
            ILogger<UploadController> logger,
            IEntitlementService entitlementService)
        {
            _environment = environment;
            _logger = logger;
            _entitlementService = entitlementService;
            _logger.LogInformation("UploadController initialized.");
        }

        [HttpGet]
        public IActionResult TestAccessibility()
        {
            return Ok(new { message = "UploadController is accessible via GET" });
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
        {
            _logger.LogInformation("UploadFile called via POST.");

            var file = request.File;

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds 5MB limit" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return BadRequest(new { message = $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", allowedExtensions)}" });

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{uniqueFileName}";

            _logger.LogInformation("File uploaded successfully: {FileName}", uniqueFileName);
            await _entitlementService.RecordUsageAsync(
                "storage_mb",
                Math.Round((decimal)file.Length / (1024m * 1024m), 4),
                "mb",
                "upload");
            return Ok(new { url = fileUrl });
        }
    }
}
