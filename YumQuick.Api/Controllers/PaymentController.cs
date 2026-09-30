using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.V2.Core;
using System.IO;
using YumQuick.Core.Enums;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("stripe-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var endpointSecret = _config["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    if (paymentIntent.Metadata.TryGetValue("OrderId", out string orderIdString) && int.TryParse(orderIdString, out int orderId))
                    {
                        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                        if (order != null)
                        {
                            order.PaymentStatus = PaymentStatus.Paid;
                            if (order.Status == OrderStatus.Pending)
                            {
                                order.Status = OrderStatus.Preparing;
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent.Metadata.TryGetValue("OrderId", out string orderIdString) && int.TryParse(orderIdString, out int orderId))
                    {
                        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                        if (order != null)
                        {
                            order.PaymentStatus = PaymentStatus.Failed;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException)
            {
                return BadRequest();
            }
        }
    }
}