using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Core.Enums;
using YumQuick.Core.Interfaces;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFawryPaymentService _fawryService;
        private readonly FawrySettings _fawrySettings;

        public OrdersController(
            ApplicationDbContext context,
            IFawryPaymentService fawryService,
            IOptions<FawrySettings> fawrySettings)
        {
            _context = context;
            _fawryService = fawryService;
            _fawrySettings = fawrySettings.Value;
        }

        // POST: api/Orders/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId);
            if (address == null) return BadRequest("Invalid delivery address.");

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(sv => sv.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Your cart is empty.");

            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart.Items)
            {
                var productFinalPrice = cartItem.Product.OriginalPrice - (cartItem.Product.OriginalPrice * (cartItem.Product.DiscountPercent / 100m));
                var variantsPrice = cartItem.SelectedVariants.Sum(v => v.Variant.ExtraPrice);

                var unitPrice = productFinalPrice + variantsPrice;
                subtotal += unitPrice * cartItem.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPriceSnapshot = unitPrice,
                    SelectedVariants = cartItem.SelectedVariants.Select(v => new OrderItemVariant
                    {
                        VariantId = v.VariantId,
                        ExtraPriceSnapshot = v.Variant.ExtraPrice
                    }).ToList()
                };
                orderItems.Add(orderItem);
            }

            decimal discountAmount = 0;
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                appliedCoupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                    c.Code == dto.CouponCode &&
                    c.IsActive &&
                    (c.ExpiryDate == null || c.ExpiryDate > DateTime.UtcNow) &&
                    c.CurrentUses < c.MaxUses);

                if (appliedCoupon != null)
                {
                    discountAmount = subtotal * (appliedCoupon.DiscountPercentage / 100m);
                    appliedCoupon.CurrentUses++;
                }
                else
                {
                    return BadRequest("Invalid or expired coupon.");
                }
            }

            decimal deliveryFee = 15.00m;
            decimal taxFee = (subtotal - discountAmount) * 0.14m;

            var order = new Order
            {
                CustomerId = userId,
                AddressId = dto.AddressId,
                Status = OrderStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                TaxFee = taxFee,
                CouponId = appliedCoupon?.Id,
                DiscountAmount = discountAmount,
                TotalAmount = (subtotal - discountAmount) + deliveryFee + taxFee,
                Items = orderItems
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync(); // تم حفظ الطلب وأخذ Id

            // 1. الدفع كاش
            if (dto.PaymentMethod == "Cash")
            {
                return Ok(new { Message = "Order placed successfully", OrderId = order.Id });
            }

            // 2. الدفع ببطاقة جديدة
            else if (dto.PaymentMethod == "NewCard")
            {
                var merchantRefNum = $"YUM-{order.Id}-{DateTime.UtcNow.Ticks}";
                order.FawryRefNumber = merchantRefNum;
                await _context.SaveChangesAsync();

                var signature = _fawryService.GenerateSignature(merchantRefNum, userId, order.TotalAmount);

                return Ok(new
                {
                    Message = "Initiate Payment",
                    OrderId = order.Id,
                    FawryRefNumber = merchantRefNum,
                    MerchantCode = _fawrySettings.MerchantCode,
                    Amount = order.TotalAmount.ToString("0.00"),
                    Signature = signature
                });
            }

            // 3. الدفع ببطاقة محفوظة
            else if (dto.PaymentMethod == "SavedCard")
            {
                if (!dto.SavedCardId.HasValue) return BadRequest("SavedCardId is required.");

                var savedCard = await _context.SavedCards.FirstOrDefaultAsync(c => c.Id == dto.SavedCardId && c.UserId == userId);
                if (savedCard == null) return NotFound("Saved card not found.");

                var merchantRefNum = $"YUM-{order.Id}-TOK-{DateTime.UtcNow.Ticks}";
                order.FawryRefNumber = merchantRefNum;
                await _context.SaveChangesAsync();

                // في فوري، عشان تخصم من كارت محفوظ (Token)، بتحتاج تعمل Request من الباك إند بتاعك لسيرفر فوري مباشرة.
                var paymentResult = await _fawryService.ChargeTokenizedCardAsync(merchantRefNum, savedCard.Token, order.TotalAmount, userId);

                if (paymentResult)
                {
                    // العملية قُبلت مبدئياً، فوري سترسل تأكيداً نهائياً على الـ Webhook
                    return Ok(new { Message = "Order placed. Payment processing via saved card.", OrderId = order.Id });
                }
                else
                {
                    // إذا فشل الاتصال بفوري أو رُفض الكارت
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    return BadRequest("Failed to process payment with the saved card.");
                }
            }

            return BadRequest("Invalid Payment Method");
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] string? filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.Orders
                .Where(o => o.CustomerId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter))
            {
                if (filter.ToLower() == "completed")
                {
                    query = query.Where(o => o.Status == OrderStatus.Delivered);
                }
                else if (filter.ToLower() == "canceled")
                {
                    query = query.Where(o => o.Status == OrderStatus.Cancelled);
                }
                else if (filter.ToLower() == "active")
                {
                    query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
                }
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            return Ok(orders);
        }
        
        // GET: api/Orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(sv => sv.Variant)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null) return NotFound(new { Message = "Order not found." });

            var result = new
            {
                order.Id,
                order.Status,
                order.Subtotal,
                order.DeliveryFee,
                order.TaxFee,
                order.DiscountAmount,
                order.TotalAmount,
                order.CreatedAt,
                Address = new { order.Address.Label, order.Address.FullAddress },
                Items = order.Items.Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product.Name,
                    ProductImage = i.Product.ImageUrl,
                    i.Quantity,
                    i.UnitPriceSnapshot,
                    Variants = i.SelectedVariants.Select(v => new
                    {
                        v.Variant.Name,
                        v.ExtraPriceSnapshot
                    })
                })
            };

            return Ok(result);
        }

        // PUT: api/Orders/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null) return NotFound(new { Message = "Order not found." });

            if (order.Status != OrderStatus.Pending)
                return BadRequest(new { Message = "Order cannot be canceled at this stage." });

            var reasonExists = await _context.CancelReasons.AnyAsync(r => r.Id == dto.CancelReasonId && r.IsActive);
            if (!reasonExists) return BadRequest(new { Message = "Invalid cancel reason." });

            order.Status = OrderStatus.Cancelled;
            order.CancelReasonId = dto.CancelReasonId;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order cancelled successfully." });
        }

        // POST: api/Orders/{id}/prepare
        [HttpPost("{id}/prepare")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> MarkOrderAsPreparing(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { Message = "Order not found." });

            if (order.Status != OrderStatus.Pending)
                return BadRequest(new { Message = "Order must be 'Pending' to start preparing." });

            order.Status = OrderStatus.Preparing;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order is now being prepared." });
        }

        // POST: api/Orders/{id}/ready
        [HttpPost("{id}/ready")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> MarkOrderAsReady(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { Message = "Order not found." });

            if (order.Status != OrderStatus.Preparing)
                return BadRequest(new { Message = "Order must be 'Preparing' before it can be marked as ready." });

            order.Status = OrderStatus.ReadyForDelivery;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order is ready for delivery." });
        }
    }
}