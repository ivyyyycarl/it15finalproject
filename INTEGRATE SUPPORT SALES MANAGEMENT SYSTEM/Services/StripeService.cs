using Stripe;
using Stripe.Checkout;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services
{
    public interface IStripeService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency = "php");
        Task<Refund> CreateRefundAsync(string paymentIntentId, decimal? amount = null, string? reason = null);
        Task<Session> CreateSubscriptionCheckoutSessionAsync(
            string planName,
            decimal amount,
            string currency,
            bool isAnnual,
            string? customerEmail,
            string successUrl,
            string cancelUrl,
            Dictionary<string, string>? metadata = null,
            string? stripeCustomerId = null);
        Task<Subscription> ChangeSubscriptionPlanAsync(string stripeSubscriptionId, string stripePriceId, bool prorate = true);
        Task<Subscription> CancelSubscriptionAsync(string stripeSubscriptionId, bool cancelAtPeriodEnd = true);
        Task<Subscription> ReactivateSubscriptionAsync(string stripeSubscriptionId);
    }

    public class StripeService : IStripeService
    {
        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency = "php")
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
            }

            // Stripe expects the smallest currency unit as an integer.
            // Rounding avoids fractional-cent precision issues from decimal arithmetic.
            var minorUnits = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

            var options = new PaymentIntentCreateOptions
            {
                Amount = minorUnits,
                Currency = currency.ToLowerInvariant(),
                // Keep checkout predictable for test-card flow.
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }

        public async Task<Refund> CreateRefundAsync(string paymentIntentId, decimal? amount = null, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                throw new ArgumentException("Payment intent ID is required.", nameof(paymentIntentId));
            }

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId.Trim(),
                Metadata = new Dictionary<string, string>()
            };

            if (amount.HasValue)
            {
                if (amount.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be greater than zero.");
                }

                options.Amount = (long)Math.Round(amount.Value * 100m, MidpointRounding.AwayFromZero);
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                options.Metadata["reason"] = reason.Trim();
            }

            var service = new RefundService();
            return await service.CreateAsync(options);
        }

        public async Task<Session> CreateSubscriptionCheckoutSessionAsync(
            string planName,
            decimal amount,
            string currency,
            bool isAnnual,
            string? customerEmail,
            string successUrl,
            string cancelUrl,
            Dictionary<string, string>? metadata = null,
            string? stripeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                throw new ArgumentException("Plan name is required.", nameof(planName));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Subscription amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
            {
                throw new ArgumentException("Checkout success/cancel URLs are required.");
            }

            var minorUnits = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "php" : currency.Trim().ToLowerInvariant();
            var interval = isAnnual ? "year" : "month";

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata ?? new Dictionary<string, string>(),
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = normalizedCurrency,
                            UnitAmount = minorUnits,
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = interval
                            },
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{planName} ({(isAnnual ? "Annual" : "Monthly")})"
                            }
                        }
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                options.Customer = stripeCustomerId.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                options.CustomerEmail = customerEmail.Trim();
            }

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<Subscription> ChangeSubscriptionPlanAsync(string stripeSubscriptionId, string stripePriceId, bool prorate = true)
        {
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                throw new ArgumentException("Stripe subscription ID is required.", nameof(stripeSubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(stripePriceId))
            {
                throw new ArgumentException("Stripe price ID is required.", nameof(stripePriceId));
            }

            var service = new SubscriptionService();
            var current = await service.GetAsync(stripeSubscriptionId.Trim());
            var itemId = current.Items.Data.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new InvalidOperationException("Stripe subscription has no updatable items.");
            }

            var updateOptions = new SubscriptionUpdateOptions
            {
                ProrationBehavior = prorate ? "create_prorations" : "none",
                Items = new List<SubscriptionItemOptions>
                {
                    new()
                    {
                        Id = itemId,
                        Price = stripePriceId.Trim()
                    }
                }
            };

            return await service.UpdateAsync(stripeSubscriptionId.Trim(), updateOptions);
        }

        public async Task<Subscription> CancelSubscriptionAsync(string stripeSubscriptionId, bool cancelAtPeriodEnd = true)
        {
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                throw new ArgumentException("Stripe subscription ID is required.", nameof(stripeSubscriptionId));
            }

            var service = new SubscriptionService();
            if (!cancelAtPeriodEnd)
            {
                return await service.CancelAsync(stripeSubscriptionId.Trim(), null);
            }

            var updateOptions = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };
            return await service.UpdateAsync(stripeSubscriptionId.Trim(), updateOptions);
        }

        public async Task<Subscription> ReactivateSubscriptionAsync(string stripeSubscriptionId)
        {
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                throw new ArgumentException("Stripe subscription ID is required.", nameof(stripeSubscriptionId));
            }

            var service = new SubscriptionService();
            var updateOptions = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };
            return await service.UpdateAsync(stripeSubscriptionId.Trim(), updateOptions);
        }
    }
}
