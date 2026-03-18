using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers
{
    [ApiController]
    [Route("api/public/subscription")]
    [AllowAnonymous]
    public class PublicSubscriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public PublicSubscriptionController(ApplicationDbContext context, IUserService userService, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _userService = userService;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpGet("plans")]
        public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPublicPlans()
        {
            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.MonthlyPrice)
                .ThenBy(p => p.Name)
                .Select(p => new SubscriptionPlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description,
                    MonthlyPrice = p.MonthlyPrice,
                    AnnualPrice = p.AnnualPrice,
                    MaxUsers = p.MaxUsers,
                    MaxBranches = p.MaxBranches,
                    MaxTicketsPerMonth = p.MaxTicketsPerMonth,
                    MaxCallLogsPerMonth = p.MaxCallLogsPerMonth,
                    MaxStorageMb = p.MaxStorageMb,
                    IsSoftLimit = p.IsSoftLimit,
                    SoftLimitGracePercent = p.SoftLimitGracePercent,
                    IncludedModulesCsv = p.IncludedModulesCsv,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return Ok(plans);
        }

        [HttpPost("subscribe-company")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<CompanySubscriptionResponse>> SubscribeCompany([FromBody] CompanySubscriptionRequest request)
        {
            try
            {
                var response = await CreateCompanySubscriptionAsync(request, null, null);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("checkout-session")]
        public async Task<ActionResult<object>> CreateCheckoutSession([FromBody] CompanySubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AdminEmail) || string.IsNullOrWhiteSpace(request.AdminPassword))
            {
                return BadRequest(new { message = "Admin email and password are required." });
            }

            var plan = await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
            if (plan == null)
            {
                return BadRequest(new { message = "Selected subscription plan is invalid or inactive." });
            }

            var amount = request.BillingCycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice;
            if (amount <= 0)
            {
                return BadRequest(new { message = "Selected plan amount is invalid." });
            }

            var appBaseUrl = _configuration["App:BaseUrl"];
            if (string.IsNullOrWhiteSpace(appBaseUrl))
            {
                appBaseUrl = _configuration["App__BaseUrl"];
            }
            if (string.IsNullOrWhiteSpace(appBaseUrl))
            {
                var req = HttpContext.Request;
                appBaseUrl = $"{req.Scheme}://{req.Host.Value}";
            }

            var normalizedAdminEmail = request.AdminEmail.Trim().ToLowerInvariant();
            var stripeSecretKey = _configuration["Stripe:SecretKey"]
                ?? _configuration["Stripe__SecretKey"]
                ?? Environment.GetEnvironmentVariable("Stripe__SecretKey");
            var stripePublishableKey = _configuration["Stripe:PublishableKey"]
                ?? _configuration["Stripe__PublishableKey"]
                ?? Environment.GetEnvironmentVariable("Stripe__PublishableKey");

            bool allowMock = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() 
                             || _configuration.GetValue<bool>("Stripe:AllowMockInProduction")
                             || _configuration.GetValue<bool>("Stripe__AllowMockInProduction")
                             || (string.IsNullOrWhiteSpace(stripeSecretKey) && string.IsNullOrWhiteSpace(stripePublishableKey));

            if (string.IsNullOrWhiteSpace(stripeSecretKey) || string.IsNullOrWhiteSpace(stripePublishableKey))
            {
                if (allowMock)
                {
                    var activated = await CreateCompanySubscriptionAsync(
                        request,
                        stripeCustomerId: "mock_customer",
                        stripeSessionId: $"mock_session_{Guid.NewGuid():N}");

                    var mockSuccessUrl =
                        $"/subscribe/success" +
                        $"?mock=1" +
                        $"&tenantName={Uri.EscapeDataString(activated.TenantName)}" +
                        $"&planName={Uri.EscapeDataString(activated.PlanName)}" +
                        $"&adminUserId={activated.AdminUserId}" +
                        $"&tenantSubscriptionId={activated.TenantSubscriptionId}";

                    return Ok(new
                    {
                        checkoutUrl = mockSuccessUrl,
                        sessionId = $"mock_{activated.TenantSubscriptionId}"
                    });
                }

                return StatusCode(500, new
                {
                    message = "Stripe keys are not configured for this environment."
                });
            }

            StripeConfiguration.ApiKey = stripeSecretKey;

            // Retry-safe flow: if an unfinished onboarding exists for this email, resume by issuing a fresh checkout session.
            var existingPending = await _context.PendingSubscriptionOnboardings
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => !p.IsCompleted && p.AdminEmail.ToLower() == normalizedAdminEmail);

            if (existingPending != null)
            {
                var existingPlan = await _context.SubscriptionPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == existingPending.SubscriptionPlanId && p.IsActive);
                if (existingPlan == null)
                {
                    return BadRequest(new { message = "Existing pending subscription has an inactive/invalid plan. Please contact support." });
                }

                var existingIsAnnual = string.Equals(existingPending.BillingCycle, nameof(BillingCycle.Annual), StringComparison.OrdinalIgnoreCase);
                var existingAmount = existingIsAnnual ? existingPlan.AnnualPrice : existingPlan.MonthlyPrice;
                if (existingAmount <= 0)
                {
                    return BadRequest(new { message = "Existing pending subscription has invalid pricing." });
                }

                var resumedSessionOptions = new SessionCreateOptions
                {
                    Mode = "subscription",
                    SuccessUrl = $"{appBaseUrl.TrimEnd('/')}/subscribe/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{appBaseUrl.TrimEnd('/')}/subscribe?canceled=true",
                    CustomerEmail = existingPending.AdminEmail,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "php",
                                UnitAmount = (long)Math.Round(existingAmount * 100m, MidpointRounding.AwayFromZero),
                                Recurring = new SessionLineItemPriceDataRecurringOptions
                                {
                                    Interval = existingIsAnnual ? "year" : "month"
                                },
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{existingPlan.Name} ({existingPending.BillingCycle})",
                                    Description = $"Company onboarding subscription for {existingPending.CompanyName.Trim()}"
                                }
                            }
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["pendingId"] = existingPending.Id.ToString(),
                        ["companyName"] = existingPending.CompanyName,
                        ["subscriptionPlanId"] = existingPending.SubscriptionPlanId.ToString(),
                        ["billingCycle"] = existingPending.BillingCycle
                    }
                };

                var resumedSessionService = new SessionService();
                Session resumedSession;
                try
                {
                    resumedSession = await resumedSessionService.CreateAsync(resumedSessionOptions);
                }
                catch (StripeException ex)
                {
                    return StatusCode(502, new
                    {
                        message = "Unable to create Stripe checkout session.",
                        detail = ex.StripeError?.Message ?? ex.Message
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new
                    {
                        message = "Unable to initialize checkout session.",
                        detail = ex.Message
                    });
                }
                existingPending.CheckoutSessionId = resumedSession.Id;
                existingPending.StripeCustomerId = resumedSession.CustomerId;
                existingPending.CheckoutStatus = resumedSession.Status ?? "created";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    checkoutUrl = resumedSession.Url,
                    sessionId = resumedSession.Id
                });
            }

            var existingEmail = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == normalizedAdminEmail);
            if (existingEmail)
            {
                return Conflict(new { message = "Admin email is already in use." });
            }

            // Create inactive admin account first so checkout completion can be finalized by webhook
            // even if user closes the browser before returning to success page.
            var preCreatedUser = new User
            {
                FirstName = request.AdminFirstName.Trim(),
                LastName = request.AdminLastName.Trim(),
                Email = request.AdminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
                Phone = request.ContactPhone?.Trim() ?? string.Empty,
                Role = UserRole.Admin,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(preCreatedUser);
            await _context.SaveChangesAsync();

            var pending = new PendingSubscriptionOnboarding
            {
                CompanyName = request.CompanyName.Trim(),
                AdminEmail = request.AdminEmail.Trim(),
                AdminFirstName = request.AdminFirstName.Trim(),
                AdminLastName = request.AdminLastName.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                SubscriptionPlanId = request.SubscriptionPlanId,
                BillingCycle = request.BillingCycle.ToString(),
                InitialBranchName = request.InitialBranchName?.Trim(),
                AdminUserId = preCreatedUser.Id,
                CheckoutStatus = "created",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.PendingSubscriptionOnboardings.Add(pending);
            await _context.SaveChangesAsync();

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = $"{appBaseUrl.TrimEnd('/')}/subscribe/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{appBaseUrl.TrimEnd('/')}/subscribe?canceled=true",
                CustomerEmail = request.AdminEmail.Trim(),
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "php",
                            UnitAmount = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero),
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = request.BillingCycle == BillingCycle.Annual ? "year" : "month"
                            },
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{plan.Name} ({request.BillingCycle})",
                                Description = $"Company onboarding subscription for {request.CompanyName.Trim()}"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["pendingId"] = pending.Id.ToString(),
                    ["companyName"] = pending.CompanyName,
                    ["subscriptionPlanId"] = pending.SubscriptionPlanId.ToString(),
                    ["billingCycle"] = pending.BillingCycle
                }
            };

            var service = new SessionService();
            Session checkoutSession;
            try
            {
                checkoutSession = await service.CreateAsync(sessionOptions);
            }
            catch (StripeException ex)
            {
                _context.PendingSubscriptionOnboardings.Remove(pending);
                _context.Users.Remove(preCreatedUser);
                await _context.SaveChangesAsync();
                return StatusCode(502, new
                {
                    message = "Unable to create Stripe checkout session.",
                    detail = ex.StripeError?.Message ?? ex.Message
                });
            }
            catch
            {
                // Cleanup if Stripe session creation failed.
                _context.PendingSubscriptionOnboardings.Remove(pending);
                _context.Users.Remove(preCreatedUser);
                await _context.SaveChangesAsync();
                return StatusCode(500, new { message = "Unable to initialize checkout session." });
            }

            pending.CheckoutSessionId = checkoutSession.Id;
            pending.StripeCustomerId = checkoutSession.CustomerId;
            pending.CheckoutStatus = checkoutSession.Status ?? "created";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                checkoutUrl = checkoutSession.Url,
                sessionId = checkoutSession.Id
            });
        }

        [HttpPost("confirm-checkout")]
        public async Task<ActionResult<CompanySubscriptionResponse>> ConfirmCheckout([FromBody] ConfirmCheckoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new { message = "Session ID is required." });
            }

            var service = new SessionService();
            var checkoutSession = await service.GetAsync(request.SessionId.Trim());
            if (checkoutSession == null)
            {
                return NotFound(new { message = "Checkout session not found." });
            }

            try
            {
                var response = await ActivateFromCheckoutSessionAsync(checkoutSession);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"]
                ?? _configuration["Stripe__WebhookSecret"]
                ?? Environment.GetEnvironmentVariable("Stripe__WebhookSecret");
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                return BadRequest(new { message = "Stripe webhook secret is not configured." });
            }

            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            var signatureHeader = Request.Headers["Stripe-Signature"];
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, webhookSecret);
            }
            catch
            {
                return BadRequest(new { message = "Invalid Stripe signature." });
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                    {
                        var session = stripeEvent.Data.Object as Session;
                        if (session != null)
                        {
                            await HandleCheckoutSessionCompletedAsync(session);
                        }
                        break;
                    }
                    case "customer.subscription.updated":
                    case "customer.subscription.deleted":
                    {
                        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                        if (subscription != null)
                        {
                            await ApplySubscriptionLifecycleUpdateAsync(subscription);
                        }
                        break;
                    }
                    case "invoice.paid":
                    case "invoice.payment_succeeded":
                    case "invoice.payment_failed":
                    {
                        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                        if (invoice != null)
                        {
                            await ApplyInvoiceLifecycleUpdateAsync(invoice);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Keep webhook idempotent and retryable by returning 200.
            }

            return Ok();
        }

        private async Task HandleCheckoutSessionCompletedAsync(Session checkoutSession)
        {
            var flow = checkoutSession.Metadata != null &&
                       checkoutSession.Metadata.TryGetValue("flow", out var flowValue)
                ? flowValue
                : null;

            if (string.Equals(flow, "plan_change", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyPlanChangeFromCheckoutSessionAsync(checkoutSession);
                return;
            }

            await ActivateFromCheckoutSessionAsync(checkoutSession);
        }

        private async Task<string> GenerateUniqueBranchCodeAsync(string companyName)
        {
            var baseCode = new string(companyName
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .Take(6)
                .ToArray());

            if (string.IsNullOrWhiteSpace(baseCode))
            {
                baseCode = "TENANT";
            }

            for (var i = 0; i < 20; i++)
            {
                var code = $"{baseCode}-{Random.Shared.Next(100, 999)}";
                var exists = await _context.Branches.AnyAsync(b => b.Code == code);
                if (!exists)
                {
                    return code;
                }
            }

            return $"{baseCode}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }

        private async Task<CompanySubscriptionResponse> CreateCompanySubscriptionAsync(
            CompanySubscriptionRequest request,
            string? stripeCustomerId,
            string? stripeSessionId)
        {
            var existingUser = await _userService.GetUserByEmailAsync(request.AdminEmail);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Admin email is already registered.");
            }

            var selectedPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
            if (selectedPlan == null)
            {
                throw new InvalidOperationException("Selected subscription plan is invalid or inactive.");
            }

            var createAdminDto = new CreateUserDto
            {
                FirstName = request.AdminFirstName.Trim(),
                LastName = request.AdminLastName.Trim(),
                Email = request.AdminEmail.Trim(),
                Password = request.AdminPassword,
                Phone = request.ContactPhone?.Trim(),
                Role = UserRole.Admin,
                Company = request.CompanyName.Trim()
            };

            var createdAdmin = await _userService.CreateUserAsync(createAdminDto, "Self-Service Subscription");

            var branch = new Branch
            {
                Name = string.IsNullOrWhiteSpace(request.InitialBranchName)
                    ? $"{request.CompanyName.Trim()} Main"
                    : request.InitialBranchName.Trim(),
                Code = await GenerateUniqueBranchCodeAsync(request.CompanyName),
                IsActive = true,
                Country = "Philippines",
                CreatedAt = DateTime.UtcNow
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            var adminUserEntity = await _context.Users.FirstOrDefaultAsync(u => u.Id == createdAdmin.Id);
            if (adminUserEntity != null)
            {
                adminUserEntity.BranchId = branch.Id;
                adminUserEntity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var startsAt = DateTime.UtcNow;
            var nextBillingAt = request.BillingCycle == BillingCycle.Annual
                ? startsAt.AddYears(1)
                : startsAt.AddMonths(1);

            var tenantSubscription = new TenantSubscription
            {
                TenantName = request.CompanyName.Trim(),
                SubscriptionPlanId = selectedPlan.Id,
                Status = SubscriptionStatus.Active,
                StartsAt = startsAt,
                NextBillingAt = nextBillingAt,
                CurrentPeriodStart = startsAt,
                CurrentPeriodEnd = nextBillingAt,
                BillingCycle = request.BillingCycle.ToString(),
                Currency = "PHP",
                UnitPrice = request.BillingCycle == BillingCycle.Annual ? selectedPlan.AnnualPrice : selectedPlan.MonthlyPrice,
                AutoRenew = true,
                StripeCustomerId = stripeCustomerId,
                StripeSubscriptionId = stripeSessionId,
                LastPaymentStatus = "paid",
                LastPaymentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.TenantSubscriptions.Add(tenantSubscription);
            await _context.SaveChangesAsync();

            await _userService.LogAuditActionAsync(
                "Company Subscription Started",
                $"Company '{tenantSubscription.TenantName}' subscribed to '{selectedPlan.Name}' ({request.BillingCycle}).",
                createdAdmin.Id);

            return new CompanySubscriptionResponse
            {
                TenantSubscriptionId = tenantSubscription.Id,
                AdminUserId = createdAdmin.Id,
                BranchId = branch.Id,
                TenantName = tenantSubscription.TenantName,
                PlanName = selectedPlan.Name,
                Message = "Payment verified and subscription created. You can now log in with the admin account."
            };
        }

        private async Task<CompanySubscriptionResponse> ActivateFromCheckoutSessionAsync(Session checkoutSession)
        {
            if (!string.Equals(checkoutSession.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Checkout session is not completed yet.");
            }

            var stripeSubscriptionId = checkoutSession.SubscriptionId ?? checkoutSession.Id;
            var existing = await _context.TenantSubscriptions
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
            if (existing != null)
            {
                return new CompanySubscriptionResponse
                {
                    TenantSubscriptionId = existing.Id,
                    AdminUserId = 0,
                    BranchId = 0,
                    TenantName = existing.TenantName,
                    PlanName = existing.SubscriptionPlan?.Name ?? "Unknown",
                    Message = "Subscription was already activated for this checkout session."
                };
            }

            var pending = await _context.PendingSubscriptionOnboardings
                .FirstOrDefaultAsync(p => p.CheckoutSessionId == checkoutSession.Id);
            if (pending == null)
            {
                throw new InvalidOperationException("Pending onboarding record not found for this checkout session.");
            }

            if (pending.IsCompleted)
            {
                var tenant = await _context.TenantSubscriptions
                    .AsNoTracking()
                    .Include(s => s.SubscriptionPlan)
                    .FirstOrDefaultAsync(s => s.StripeSubscriptionId == checkoutSession.Id);
                if (tenant != null)
                {
                    return new CompanySubscriptionResponse
                    {
                        TenantSubscriptionId = tenant.Id,
                        AdminUserId = pending.AdminUserId,
                        BranchId = 0,
                        TenantName = tenant.TenantName,
                        PlanName = tenant.SubscriptionPlan?.Name ?? "Unknown",
                        Message = "Subscription was already activated for this checkout session."
                    };
                }
            }

            var selectedPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == pending.SubscriptionPlanId && p.IsActive);
            if (selectedPlan == null)
            {
                throw new InvalidOperationException("Selected subscription plan is invalid or inactive.");
            }

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == pending.AdminUserId);
            if (adminUser == null)
            {
                throw new InvalidOperationException("Pending admin account not found.");
            }

            var branch = new Branch
            {
                Name = string.IsNullOrWhiteSpace(pending.InitialBranchName)
                    ? $"{pending.CompanyName.Trim()} Main"
                    : pending.InitialBranchName.Trim(),
                Code = await GenerateUniqueBranchCodeAsync(pending.CompanyName),
                IsActive = true,
                Country = "Philippines",
                CreatedAt = DateTime.UtcNow
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            adminUser.BranchId = branch.Id;
            adminUser.IsActive = true;
            adminUser.UpdatedAt = DateTime.UtcNow;

            var startsAt = DateTime.UtcNow;
            var isAnnual = string.Equals(pending.BillingCycle, nameof(BillingCycle.Annual), StringComparison.OrdinalIgnoreCase);
            var nextBillingAt = isAnnual
                ? startsAt.AddYears(1)
                : startsAt.AddMonths(1);

            var tenantSubscription = new TenantSubscription
            {
                TenantName = pending.CompanyName.Trim(),
                SubscriptionPlanId = selectedPlan.Id,
                Status = SubscriptionStatus.Active,
                StartsAt = startsAt,
                NextBillingAt = nextBillingAt,
                CurrentPeriodStart = startsAt,
                CurrentPeriodEnd = nextBillingAt,
                BillingCycle = pending.BillingCycle,
                Currency = "PHP",
                UnitPrice = isAnnual ? selectedPlan.AnnualPrice : selectedPlan.MonthlyPrice,
                AutoRenew = true,
                StripeCustomerId = checkoutSession.CustomerId,
                StripeSubscriptionId = stripeSubscriptionId,
                LastPaymentStatus = string.IsNullOrWhiteSpace(checkoutSession.PaymentStatus) ? "pending" : checkoutSession.PaymentStatus,
                LastPaymentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.TenantSubscriptions.Add(tenantSubscription);

            pending.IsCompleted = true;
            pending.CompletedAt = DateTime.UtcNow;
            pending.CheckoutStatus = checkoutSession.PaymentStatus ?? "paid";
            pending.StripeCustomerId = checkoutSession.CustomerId;

            await _context.SaveChangesAsync();

            await _userService.LogAuditActionAsync(
                "Company Subscription Activated",
                $"Company '{tenantSubscription.TenantName}' activated '{selectedPlan.Name}' via Stripe checkout.",
                adminUser.Id);

            try
            {
                var loginUrl = BuildLoginUrl();
                await _emailService.SendCompanySubscriptionActivatedEmailAsync(
                    adminUser.Email,
                    adminUser.FirstName,
                    tenantSubscription.TenantName,
                    selectedPlan.Name,
                    tenantSubscription.NextBillingAt ?? DateTime.UtcNow.AddMonths(1),
                    loginUrl);
            }
            catch
            {
            }

            return new CompanySubscriptionResponse
            {
                TenantSubscriptionId = tenantSubscription.Id,
                AdminUserId = adminUser.Id,
                BranchId = branch.Id,
                TenantName = tenantSubscription.TenantName,
                PlanName = selectedPlan.Name,
                Message = "Payment verified and subscription created. You can now log in with the admin account."
            };
        }

        private async Task ApplyPlanChangeFromCheckoutSessionAsync(Session checkoutSession)
        {
            if (!string.Equals(checkoutSession.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (checkoutSession.Metadata == null ||
                !checkoutSession.Metadata.TryGetValue("tenantSubscriptionId", out var tenantSubscriptionIdText) ||
                !int.TryParse(tenantSubscriptionIdText, out var tenantSubscriptionId) ||
                !checkoutSession.Metadata.TryGetValue("subscriptionPlanId", out var subscriptionPlanIdText) ||
                !int.TryParse(subscriptionPlanIdText, out var subscriptionPlanId))
            {
                return;
            }

            var tenantSubscription = await _context.TenantSubscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == tenantSubscriptionId);
            if (tenantSubscription == null)
            {
                return;
            }

            var selectedPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == subscriptionPlanId && p.IsActive);
            if (selectedPlan == null)
            {
                return;
            }

            var billingCycle = checkoutSession.Metadata.TryGetValue("billingCycle", out var cycle)
                ? (string.Equals(cycle, "Annual", StringComparison.OrdinalIgnoreCase) ? "Annual" : "Monthly")
                : "Monthly";

            tenantSubscription.SubscriptionPlanId = selectedPlan.Id;
            tenantSubscription.BillingCycle = billingCycle;
            tenantSubscription.UnitPrice = billingCycle == "Annual" ? selectedPlan.AnnualPrice : selectedPlan.MonthlyPrice;
            tenantSubscription.Currency = "PHP";
            tenantSubscription.LastPaymentStatus = string.IsNullOrWhiteSpace(checkoutSession.PaymentStatus) ? "paid" : checkoutSession.PaymentStatus;
            tenantSubscription.LastPaymentAt = DateTime.UtcNow;
            tenantSubscription.Status = SubscriptionStatus.Active;
            tenantSubscription.AutoRenew = true;
            tenantSubscription.StripeCustomerId = string.IsNullOrWhiteSpace(checkoutSession.CustomerId)
                ? tenantSubscription.StripeCustomerId
                : checkoutSession.CustomerId;
            tenantSubscription.StripeSubscriptionId = string.IsNullOrWhiteSpace(checkoutSession.SubscriptionId)
                ? tenantSubscription.StripeSubscriptionId
                : checkoutSession.SubscriptionId;

            var now = DateTime.UtcNow;
            tenantSubscription.CurrentPeriodStart = now;
            tenantSubscription.CurrentPeriodEnd = billingCycle == "Annual" ? now.AddYears(1) : now.AddMonths(1);
            tenantSubscription.NextBillingAt = tenantSubscription.CurrentPeriodEnd;
            tenantSubscription.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        private string BuildLoginUrl()
        {
            var appBaseUrl = _configuration["App:BaseUrl"];
            if (string.IsNullOrWhiteSpace(appBaseUrl))
            {
                var req = HttpContext?.Request;
                if (req != null)
                {
                    appBaseUrl = $"{req.Scheme}://{req.Host.Value}";
                }
            }

            if (string.IsNullOrWhiteSpace(appBaseUrl))
            {
                appBaseUrl = "http://localhost:5300";
            }

            return $"{appBaseUrl.TrimEnd('/')}/login";
        }

        private async Task ApplySubscriptionLifecycleUpdateAsync(Stripe.Subscription subscription)
        {
            var subscriptionId = subscription.Id;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return;
            }

            var tenantSubscription = await _context.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
            if (tenantSubscription == null)
            {
                return;
            }

            var status = (subscription.Status ?? string.Empty).ToLowerInvariant();
            tenantSubscription.Status = status switch
            {
                "active" => SubscriptionStatus.Active,
                "trialing" => SubscriptionStatus.Trial,
                "past_due" => SubscriptionStatus.PastDue,
                "canceled" => SubscriptionStatus.Canceled,
                "unpaid" => SubscriptionStatus.PastDue,
                _ => tenantSubscription.Status
            };
            tenantSubscription.AutoRenew = !subscription.CancelAtPeriodEnd;
            tenantSubscription.CanceledAt = subscription.CanceledAt;
            tenantSubscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task ApplyInvoiceLifecycleUpdateAsync(Stripe.Invoice invoice)
        {
            var stripeSubscriptionId = GetStripeStringProperty(invoice, "SubscriptionId", "Subscription");
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                return;
            }

            var tenantSubscription = await _context.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
            if (tenantSubscription == null)
            {
                return;
            }

            var normalizedStatus = (invoice.Status ?? string.Empty).ToLowerInvariant();
            tenantSubscription.LastPaymentStatus = normalizedStatus;
            tenantSubscription.LastPaymentAt = invoice.StatusTransitions?.PaidAt;
            tenantSubscription.NextBillingAt = invoice.PeriodEnd;
            if (normalizedStatus == "paid")
            {
                tenantSubscription.Status = SubscriptionStatus.Active;
            }
            else if (normalizedStatus is "open" or "uncollectible")
            {
                tenantSubscription.Status = SubscriptionStatus.PastDue;
            }

            var existingRecord = await _context.SubscriptionInvoiceRecords
                .FirstOrDefaultAsync(r => r.StripeInvoiceId == invoice.Id);
            if (existingRecord == null)
            {
                existingRecord = new SubscriptionInvoiceRecord
                {
                    TenantSubscriptionId = tenantSubscription.Id,
                    StripeInvoiceId = invoice.Id,
                    StripePaymentIntentId = GetStripeStringProperty(invoice, "PaymentIntentId", "PaymentIntent"),
                    AmountDue = invoice.AmountDue / 100m,
                    AmountPaid = invoice.AmountPaid / 100m,
                    Status = normalizedStatus,
                    DueDate = invoice.DueDate,
                    PaidAt = invoice.StatusTransitions?.PaidAt,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SubscriptionInvoiceRecords.Add(existingRecord);
            }
            else
            {
                existingRecord.StripePaymentIntentId = GetStripeStringProperty(invoice, "PaymentIntentId", "PaymentIntent");
                existingRecord.AmountDue = invoice.AmountDue / 100m;
                existingRecord.AmountPaid = invoice.AmountPaid / 100m;
                existingRecord.Status = normalizedStatus;
                existingRecord.DueDate = invoice.DueDate;
                existingRecord.PaidAt = invoice.StatusTransitions?.PaidAt;
            }

            await _context.SaveChangesAsync();
        }

        private static string? GetStripeStringProperty(object source, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var property = source.GetType().GetProperty(propertyName);
                if (property == null)
                {
                    continue;
                }

                var value = property.GetValue(source);
                if (value is string text && !string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return null;
        }
    }

    public class ConfirmCheckoutRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}
