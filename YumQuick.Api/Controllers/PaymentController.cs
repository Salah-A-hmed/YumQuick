using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
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
            var endpointSecret = _config["Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(endpointSecret))
                return StatusCode(500, "Stripe webhook secret is not configured.");

            var json = await new StreamReader(
                HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret);

                if (stripeEvent.Type != "payment_intent.succeeded" &&
                    stripeEvent.Type != "payment_intent.payment_failed")
                {
                    return Ok();
                }

                if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
                    return BadRequest();

                if (paymentIntent.Metadata == null ||
                    !paymentIntent.Metadata.TryGetValue(
                        "OrderId", out var orderIdString) ||
                    !int.TryParse(orderIdString, out var orderId))
                {
                    return BadRequest();
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Ok();

                // Basic check that the payment matches the order.
                var expectedAmount = (long)Math.Round(
                    order.TotalAmount * 100m,
                    0,
                    MidpointRounding.AwayFromZero);

                if (paymentIntent.Amount != expectedAmount ||
                    !string.Equals(
                        paymentIntent.Currency,
                        "usd",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest();
                }

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    // Do not change cancelled orders to Preparing.
                    if (order.Status == OrderStatus.Cancelled)
                        return Ok();

                    order.PaymentStatus = PaymentStatus.Paid;

                    if (order.Status == OrderStatus.Pending)
                        order.Status = OrderStatus.Preparing;
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    // A late failure event must not overwrite a successful payment.
                    if (order.PaymentStatus != PaymentStatus.Paid)
                        order.PaymentStatus = PaymentStatus.Failed;
                }

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (StripeException)
            {
                return BadRequest();
            }
        }
    }
}