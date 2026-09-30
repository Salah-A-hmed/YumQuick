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

        public async Task<string> CreatePaymentIntentAsync(Order order, string? paymentMethodToken = null)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.TotalAmount * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() },
                    { "UserId", order.CustomerId }
                }
            };

            // لو العميل اختار كارت محفوظ، بنقول لسترايب يستخدمه فوراً
            if (!string.IsNullOrEmpty(paymentMethodToken))
            {
                options.PaymentMethod = paymentMethodToken;
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return paymentIntent.ClientSecret;
        }
    }
}