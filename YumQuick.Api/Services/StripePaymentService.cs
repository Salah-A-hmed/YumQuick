using Stripe;
using YumQuick.Core.Entities;
using YumQuick.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YumQuick.Api.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _config;

        public StripePaymentService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> CreatePaymentIntentAsync(
            Order order,
            string? paymentMethodToken = null,
            string? stripeCustomerId = null)
        {
            try
            {
                var amountInCents = (long)Math.Round(
                order.TotalAmount * 100m,
                0,
                MidpointRounding.AwayFromZero);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "usd",

                    PaymentMethodTypes = new List<string>
        {
            "card"
        },
                    Customer = stripeCustomerId,

                    Metadata = new Dictionary<string, string>
        {
            { "OrderId", order.Id.ToString() },
            { "UserId", order.CustomerId }
        }
                };

                if (!string.IsNullOrWhiteSpace(paymentMethodToken))
                {
                    options.PaymentMethod = paymentMethodToken;
                    options.SetupFutureUsage = "off_session";
                }

                var service = new PaymentIntentService();

                var paymentIntent = await service.CreateAsync(options);

                return paymentIntent.ClientSecret;
            }
            catch (StripeException ex)
            {
                // التقاط خطأ سترايب بالتفصيل ورميه للكونترولر
                throw new Exception($"Stripe API Error: {ex.Message}");
            }
        }
    }
}