// Expose functions on window so Blazor JS interop can call them
// Matches usage from CustomerOrders.razor:
// - initializeStripe(publishableKey, clientSecret)
// - confirmPayment()

window.initializeStripe = function (publishableKey, clientSecret) {
    if (!window.Stripe) {
        console.error("Stripe.js not loaded");
        return;
    }

    const stripe = Stripe(publishableKey);

    const options = {
        clientSecret: clientSecret,
        appearance: { theme: 'stripe' },
    };

    const elements = stripe.elements(options);
    const paymentElement = elements.create('payment');
    const mountTarget = document.getElementById('payment-element');

    if (!mountTarget) {
        console.error("payment-element container not found");
        return;
    }

    paymentElement.mount('#payment-element');

    // Store globally for later confirm
    window.stripeInstance = stripe;
    window.stripeElements = elements;

    const maskedClientSecret = typeof clientSecret === "string" && clientSecret.length > 12
        ? `${clientSecret.slice(0, 8)}...${clientSecret.slice(-4)}`
        : "(missing)";
    console.log("[Stripe init]", {
        publishableKeyPrefix: publishableKey ? publishableKey.slice(0, 8) : "(missing)",
        clientSecretMasked: maskedClientSecret
    });
};

function sanitizeStripeError(error) {
    if (!error) return null;
    return {
        type: error.type,
        code: error.code,
        decline_code: error.decline_code,
        message: error.message,
        payment_intent: error.payment_intent ? {
            id: error.payment_intent.id,
            status: error.payment_intent.status,
            last_payment_error: error.payment_intent.last_payment_error
                ? {
                    type: error.payment_intent.last_payment_error.type,
                    code: error.payment_intent.last_payment_error.code,
                    decline_code: error.payment_intent.last_payment_error.decline_code,
                    message: error.payment_intent.last_payment_error.message
                }
                : null
        } : null
    };
}

function sanitizePaymentIntent(paymentIntent) {
    if (!paymentIntent) return null;
    return {
        id: paymentIntent.id,
        status: paymentIntent.status,
        amount: paymentIntent.amount,
        currency: paymentIntent.currency,
        last_payment_error: paymentIntent.last_payment_error
            ? {
                type: paymentIntent.last_payment_error.type,
                code: paymentIntent.last_payment_error.code,
                decline_code: paymentIntent.last_payment_error.decline_code,
                message: paymentIntent.last_payment_error.message
            }
            : null
    };
}

window.confirmPayment = async function () {
    if (!window.stripeInstance || !window.stripeElements) {
        console.error("Stripe not initialized");
        return { error: { message: "Payment not initialized" } };
    }

    const returnUrl = window.location.href;

    try {
        console.log("[Stripe confirmPayment] starting", { returnUrl });
        const { error, paymentIntent } = await window.stripeInstance.confirmPayment({
            elements: window.stripeElements,
            confirmParams: {
                return_url: returnUrl,
            },
            redirect: 'if_required'
        });

        console.log("[Stripe confirmPayment] raw result", {
            error: sanitizeStripeError(error),
            paymentIntent: sanitizePaymentIntent(paymentIntent)
        });

        if (error) {
            const fallback = "A processing error occurred. Please verify card details and try again.";
            return {
                error: {
                    ...error,
                    message: error.message || fallback
                }
            };
        }

        return { paymentIntent };
    } catch (err) {
        console.error("[Stripe confirmPayment] exception", {
            message: err?.message,
            stack: err?.stack
        });
        return {
            error: {
                message: err?.message || "Unable to process payment right now. Please try again."
            }
        };
    }
};

window.showMessage = function (messageText) {
    const messageContainer = document.querySelector("#payment-message");

    if (messageContainer) {
        messageContainer.classList.remove("hidden");
        messageContainer.textContent = messageText;

        // Auto-hide after 5 seconds
        setTimeout(function () {
            messageContainer.classList.add("hidden");
            messageContainer.textContent = "";
        }, 5000);
    } else {
        // Fallback to alert if container not found
        alert(messageText);
    }
};
