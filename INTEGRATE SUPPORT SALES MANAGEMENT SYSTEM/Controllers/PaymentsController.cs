using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public PaymentsController(
            IStripeService stripeService,
            IConfiguration configuration,
            ILogger<PaymentsController> logger,
            IWebHostEnvironment environment)
        {
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var publishableKey = _configuration["Stripe:PublishableKey"];
            if (string.IsNullOrWhiteSpace(publishableKey))
            {
                publishableKey = _configuration["Stripe__PublishableKey"];
            }

            if (!string.IsNullOrWhiteSpace(publishableKey))
            {
                return Ok(new { publishableKey });
            }

            // Allow mock mode in production if configured (or if no Stripe key is set)
            var allowMock = _configuration.GetValue<bool>("Stripe:AllowMockInProduction")
                         || _configuration.GetValue<bool>("Stripe__AllowMockInProduction")
                         || _environment.IsDevelopment();

            if (allowMock)
            {
                return Ok(new
                {
                    publishableKey = "mock_pk_production",
                    mode = "mock"
                });
            }

            return StatusCode(500, new
            {
                message = "Stripe publishable key is not configured."
            });
        }

        [HttpPost("create-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            var traceId = HttpContext.TraceIdentifier;
            try
            {
                var stripeSecretKey = _configuration["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(stripeSecretKey))
                {
                    stripeSecretKey = _configuration["Stripe__SecretKey"];
                }

                if (string.IsNullOrWhiteSpace(stripeSecretKey))
                {
                    var allowMock = _configuration.GetValue<bool>("Stripe:AllowMockInProduction")
                                 || _configuration.GetValue<bool>("Stripe__AllowMockInProduction")
                                 || _environment.IsDevelopment();

                    if (allowMock)
                    {
                        var mockPaymentIntentId = $"mock_pi_{Guid.NewGuid():N}";
                        return Ok(new
                        {
                            clientSecret = $"mock_client_secret_{Guid.NewGuid():N}",
                            paymentIntentId = mockPaymentIntentId,
                            status = "succeeded",
                            isMock = true,
                            traceId
                        });
                    }

                    return StatusCode(500, new
                    {
                        message = "Stripe secret key is not configured.",
                        traceId
                    });
                }

                _logger.LogInformation("CreatePaymentIntent started. TraceId: {TraceId}, Amount: {Amount}", traceId, request.Amount);
                var paymentIntent = await _stripeService.CreatePaymentIntentAsync(request.Amount);
                _logger.LogInformation("CreatePaymentIntent success. TraceId: {TraceId}, PaymentIntentId: {PaymentIntentId}, Status: {Status}",
                    traceId, paymentIntent.Id, paymentIntent.Status);

                return Ok(new
                {
                    clientSecret = paymentIntent.ClientSecret,
                    paymentIntentId = paymentIntent.Id,
                    status = paymentIntent.Status,
                    traceId
                });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "CreatePaymentIntent invalid input. TraceId: {TraceId}, Amount: {Amount}", traceId, request.Amount);
                return BadRequest(new
                {
                    message = ex.Message,
                    traceId
                });
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe exception during CreatePaymentIntent. TraceId: {TraceId}, StripeType: {StripeType}, StripeCode: {StripeCode}",
                    traceId,
                    ex.StripeError?.Type,
                    ex.StripeError?.Code);

                return StatusCode(502, new
                {
                    message = "Stripe payment gateway error.",
                    detail = ex.StripeError?.Message ?? ex.Message,
                    stripe = new
                    {
                        type = ex.StripeError?.Type,
                        code = ex.StripeError?.Code,
                        declineCode = ex.StripeError?.DeclineCode
                    },
                    traceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception during CreatePaymentIntent. TraceId: {TraceId}", traceId);
                return StatusCode(500, new
                {
                    message = "Unexpected server error while creating payment intent.",
                    detail = ex.Message,
                    traceId
                });
            }
        }
    }

    public class PaymentIntentRequest
    {
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}
