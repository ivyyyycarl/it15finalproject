using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Security.Claims;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/subscription")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;

        public SubscriptionController(
            ApplicationDbContext context,
            IStripeService stripeService,
            IConfiguration configuration)
        {
            _context = context;
            _stripeService = stripeService;
            _configuration = configuration;
        }

        [HttpGet("current")]
        public async Task<ActionResult<TenantSubscriptionDto>> GetCurrent()
        {
            var current = await _context.TenantSubscriptions
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (current == null || current.SubscriptionPlan == null)
            {
                return NotFound(new { message = "No subscription configured yet." });
            }

            return Ok(new TenantSubscriptionDto
            {
                Id = current.Id,
                TenantName = current.TenantName,
                Status = current.Status,
                StartsAt = current.StartsAt,
                EndsAt = current.EndsAt,
                NextBillingAt = current.NextBillingAt,
                AutoRenew = current.AutoRenew,
                BillingCycle = current.BillingCycle,
                CurrentPeriodStart = current.CurrentPeriodStart,
                CurrentPeriodEnd = current.CurrentPeriodEnd,
                Currency = current.Currency,
                UnitPrice = current.UnitPrice,
                DiscountAmount = current.DiscountAmount,
                TaxAmount = current.TaxAmount,
                LastPaymentStatus = current.LastPaymentStatus,
                LastPaymentAt = current.LastPaymentAt,
                TrialStartsAt = current.TrialStartsAt,
                TrialEndsAt = current.TrialEndsAt,
                CanceledAt = current.CanceledAt,
                CancelReason = current.CancelReason,
                SubscriptionPlanId = current.SubscriptionPlanId,
                PlanName = current.SubscriptionPlan.Name,
                PlanCode = current.SubscriptionPlan.Code,
                MaxUsers = current.SubscriptionPlan.MaxUsers,
                MaxBranches = current.SubscriptionPlan.MaxBranches,
                MaxTicketsPerMonth = current.SubscriptionPlan.MaxTicketsPerMonth,
                MaxCallLogsPerMonth = current.SubscriptionPlan.MaxCallLogsPerMonth,
                MaxStorageMb = current.SubscriptionPlan.MaxStorageMb,
                IncludedModulesCsv = current.SubscriptionPlan.IncludedModulesCsv
            });
        }

        [HttpPost("change-plan-checkout")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<CheckoutSessionResponseDto>> CreatePlanChangeCheckoutSession(
            [FromBody] CreatePlanChangeCheckoutRequest request)
        {
            if (request.SubscriptionPlanId <= 0)
            {
                return BadRequest(new { message = "A valid subscription plan is required." });
            }

            var plan = await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
            if (plan == null)
            {
                return BadRequest(new { message = "Selected subscription plan is invalid or inactive." });
            }

            var current = await _context.TenantSubscriptions
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
            if (current == null)
            {
                return NotFound(new { message = "No tenant subscription found to update." });
            }

            var billingCycle = string.Equals(request.BillingCycle, "Annual", StringComparison.OrdinalIgnoreCase)
                ? "Annual"
                : "Monthly";
            var unitPrice = billingCycle == "Annual" ? plan.AnnualPrice : plan.MonthlyPrice;
            if (unitPrice <= 0)
            {
                return BadRequest(new { message = "Selected plan has an invalid price for the chosen billing cycle." });
            }

            var baseUrl = _configuration["App:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var requestBase = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
                baseUrl = string.IsNullOrWhiteSpace(requestBase) ? "http://localhost:5000" : requestBase;
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["flow"] = "plan_change",
                ["tenantSubscriptionId"] = current.Id.ToString(),
                ["subscriptionPlanId"] = plan.Id.ToString(),
                ["billingCycle"] = billingCycle
            };

            var customerEmail = User.FindFirstValue(ClaimTypes.Email);
            var session = await _stripeService.CreateSubscriptionCheckoutSessionAsync(
                planName: plan.Name,
                amount: unitPrice,
                currency: "php",
                isAnnual: billingCycle == "Annual",
                customerEmail: customerEmail,
                successUrl: $"{baseUrl}/subscription/overview?checkout=success&session_id={{CHECKOUT_SESSION_ID}}",
                cancelUrl: $"{baseUrl}/subscription/overview?checkout=cancelled",
                metadata: metadata,
                stripeCustomerId: current.StripeCustomerId);

            return Ok(new CheckoutSessionResponseDto
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url ?? string.Empty
            });
        }

        [HttpPost("confirm-plan-change")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ConfirmPlanChange([FromBody] ConfirmPlanChangeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new { message = "SessionId is required." });
            }

            var sessionService = new SessionService();
            Session session;
            try
            {
                session = await sessionService.GetAsync(request.SessionId.Trim());
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Unable to load checkout session: {ex.Message}" });
            }

            if (!string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Checkout session is not complete yet." });
            }

            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("flow", out var flow) ||
                !string.Equals(flow, "plan_change", StringComparison.OrdinalIgnoreCase) ||
                !session.Metadata.TryGetValue("tenantSubscriptionId", out var tenantSubscriptionIdText) ||
                !int.TryParse(tenantSubscriptionIdText, out var tenantSubscriptionId) ||
                !session.Metadata.TryGetValue("subscriptionPlanId", out var subscriptionPlanIdText) ||
                !int.TryParse(subscriptionPlanIdText, out var subscriptionPlanId))
            {
                return BadRequest(new { message = "Session metadata is invalid for plan change." });
            }

            var tenantSubscription = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == tenantSubscriptionId);
            if (tenantSubscription == null)
            {
                return NotFound(new { message = "Tenant subscription not found." });
            }

            var selectedPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == subscriptionPlanId && p.IsActive);
            if (selectedPlan == null)
            {
                return BadRequest(new { message = "Selected plan is invalid or inactive." });
            }

            var billingCycle = session.Metadata.TryGetValue("billingCycle", out var cycle)
                ? (string.Equals(cycle, "Annual", StringComparison.OrdinalIgnoreCase) ? "Annual" : "Monthly")
                : "Monthly";

            var now = DateTime.UtcNow;
            tenantSubscription.SubscriptionPlanId = selectedPlan.Id;
            tenantSubscription.BillingCycle = billingCycle;
            tenantSubscription.UnitPrice = billingCycle == "Annual" ? selectedPlan.AnnualPrice : selectedPlan.MonthlyPrice;
            tenantSubscription.Currency = "PHP";
            tenantSubscription.LastPaymentStatus = string.IsNullOrWhiteSpace(session.PaymentStatus) ? "paid" : session.PaymentStatus;
            tenantSubscription.LastPaymentAt = now;
            tenantSubscription.Status = SubscriptionStatus.Active;
            tenantSubscription.AutoRenew = true;
            tenantSubscription.StripeCustomerId = string.IsNullOrWhiteSpace(session.CustomerId)
                ? tenantSubscription.StripeCustomerId
                : session.CustomerId;
            tenantSubscription.StripeSubscriptionId = string.IsNullOrWhiteSpace(session.SubscriptionId)
                ? tenantSubscription.StripeSubscriptionId
                : session.SubscriptionId;
            tenantSubscription.CurrentPeriodStart = now;
            tenantSubscription.CurrentPeriodEnd = billingCycle == "Annual" ? now.AddYears(1) : now.AddMonths(1);
            tenantSubscription.NextBillingAt = tenantSubscription.CurrentPeriodEnd;
            tenantSubscription.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Plan change applied successfully." });
        }
    }

    public class ConfirmPlanChangeRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}
