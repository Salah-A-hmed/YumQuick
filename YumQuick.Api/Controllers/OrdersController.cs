using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
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
        private readonly IPaymentService _paymentService;

        public OrdersController(ApplicationDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }


        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1. Validate payment method first
            var paymentMethod = dto.PaymentMethod?.Trim();

            if (paymentMethod != "Cash" &&
                paymentMethod != "Card" &&
                paymentMethod != "NewCard" &&
                paymentMethod != "SavedCard")
            {
                return BadRequest("Invalid Payment Method.");
            }

            // 2. Validate address
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == dto.AddressId &&
                    a.UserId == userId);

            if (address == null)
                return BadRequest("Invalid delivery address.");

            // 3. Validate saved card before creating the order
            SavedCard? savedCard = null;

            if (paymentMethod == "SavedCard")
            {
                if (!dto.SavedCardId.HasValue)
                    return BadRequest("SavedCardId is required.");

                savedCard = await _context.SavedCards
                    .FirstOrDefaultAsync(c =>
                        c.Id == dto.SavedCardId.Value &&
                        c.UserId == userId);

                if (savedCard == null)
                    return NotFound("Saved card not found.");
            }

            // 4. Load cart
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(sv => sv.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Your cart is empty.");

            // 5. Calculate subtotal and order items
            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart.Items)
            {
                var productFinalPrice = Math.Round(
                    cartItem.Product.OriginalPrice *
                    (1m - cartItem.Product.DiscountPercent / 100m),
                    2,
                    MidpointRounding.AwayFromZero);

                var variantsPrice = cartItem.SelectedVariants
                    .Sum(v => v.Variant.ExtraPrice);

                var unitPrice = Math.Round(
                    productFinalPrice + variantsPrice,
                    2,
                    MidpointRounding.AwayFromZero);

                subtotal += unitPrice * cartItem.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPriceSnapshot = unitPrice,

                    SelectedVariants = cartItem.SelectedVariants
                        .Select(v => new OrderItemVariant
                        {
                            VariantId = v.VariantId,
                            ExtraPriceSnapshot = v.Variant.ExtraPrice
                        })
                        .ToList()
                });
            }

            subtotal = Math.Round(
                subtotal, 2, MidpointRounding.AwayFromZero);

            // 6. Validate coupon
            decimal discountAmount = 0m;
            YumQuick.Core.Entities.Coupon? appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var couponCode = dto.CouponCode.Trim();

                appliedCoupon = await _context.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code == couponCode &&
                        c.IsActive &&
                        (c.ExpiryDate == null ||
                         c.ExpiryDate > DateTime.UtcNow) &&
                        c.CurrentUses < c.MaxUses);

                if (appliedCoupon == null)
                    return BadRequest("Invalid or expired coupon.");

                discountAmount = Math.Round(
                    subtotal * appliedCoupon.DiscountPercentage / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            // 7. Calculate final amounts
            const decimal deliveryFee = 15.00m;

            var taxFee = Math.Round(
                (subtotal - discountAmount) * 0.14m,
                2,
                MidpointRounding.AwayFromZero);

            var totalAmount = Math.Round(
                subtotal - discountAmount + deliveryFee + taxFee,
                2,
                MidpointRounding.AwayFromZero);

            // 8. Create order
            var order = new Order
            {
                CustomerId = userId,
                AddressId = dto.AddressId,
                Status = OrderStatus.Pending,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Pending,

                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                TaxFee = taxFee,
                CouponId = appliedCoupon?.Id,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,

                Items = orderItems
            };

            if (appliedCoupon != null)
                appliedCoupon.CurrentUses++;

            _context.Orders.Add(order);

            // Save first because Stripe metadata needs the OrderId.
            await _context.SaveChangesAsync();

            // 9. Cash payment
            if (paymentMethod == "Cash")
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Order placed successfully",
                    OrderId = order.Id
                });
            }

            // 10. Card payment
            try
            {
                var clientSecret = await _paymentService
                    .CreatePaymentIntentAsync(
                        order,
                        paymentMethod == "SavedCard"
                            ? savedCard!.Token
                            : null);

                // PaymentIntent creation succeeded; now clear the cart.
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = paymentMethod == "SavedCard"
                        ? "Initiate Payment with Saved Card"
                        : "Initiate Payment",

                    OrderId = order.Id,
                    ClientSecret = clientSecret
                });
            }
            catch (StripeException)
            {
                // Stripe could not create the PaymentIntent.
                // Remove the incomplete order and restore coupon usage.
                _context.Orders.Remove(order);

                if (appliedCoupon != null)
                    appliedCoupon.CurrentUses--;

                await _context.SaveChangesAsync();

                return StatusCode(502, new
                {
                    Message = "Unable to initiate card payment. Please try again."
                });
            }
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
        public async Task<IActionResult> CancelOrder(
            int id,
            [FromBody] CancelOrderDto dto)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

                    if (order == null)
                        return NotFound(new { Message = "Order not found." });

                    if (order.Status != OrderStatus.Pending)
                        return BadRequest(new
                        {
                            Message = "Order cannot be canceled at this stage."
                        });

                    // User selected a predefined reason
                    if (dto.CancelReasonId.HasValue)
                    {
                        var reasonExists = await _context.CancelReasons.AnyAsync(
                            r => r.Id == dto.CancelReasonId.Value && r.IsActive);

                        if (!reasonExists)
                            return BadRequest(new { Message = "Invalid cancel reason." });

                        order.CancelReasonId = dto.CancelReasonId.Value;
                    }
                    // User entered a custom reason
                    else
                    {
                        if (string.IsNullOrWhiteSpace(dto.CustomCancelReason))
                        {
                            return BadRequest(new
                            {
                                Message = "Please select a cancel reason or enter a custom reason."
                            });
                        }

                        var customReason = dto.CustomCancelReason.Trim();

                        if (customReason.Length > 500)
                        {
                            return BadRequest(new
                            {
                                Message = "Cancel reason cannot exceed 500 characters."
                            });
                        }

                        var newReason = new CancelReason
                        {
                            Text = customReason,
                            IsActive = true
                        };

                        _context.CancelReasons.Add(newReason);

                        // Save to generate the new reason ID
                        await _context.SaveChangesAsync();

                        order.CancelReasonId = newReason.Id;
                    }

                    order.Status = OrderStatus.Cancelled;

                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Order cancelled successfully." });
                }

        //POST /api/Orders/{orderId}/reorder
        [HttpPost("{orderId}/reorder")]
        public async Task<IActionResult> Reorder(
            int orderId,
            [FromBody] ReorderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1. Get the old order, making sure it belongs to this customer.
            var oldOrder = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(v => v.Variant)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId && o.CustomerId == userId);

            if (oldOrder == null)
                return NotFound(new { Message = "Order not found." });

            if (oldOrder.Items == null || !oldOrder.Items.Any())
                return BadRequest(new { Message = "The old order has no items." });

            // 2. Validate the requested action.
            var action = dto.Action?.Trim();

            if (action == null ||
                (!action.Equals("AddToCart", StringComparison.OrdinalIgnoreCase) &&
                 !action.Equals("Checkout", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new
                {
                    Message = "Action must be AddToCart or Checkout."
                });
            }

            // 3. Check that all old products are still available
            // and that their variants still belong to those products.
            foreach (var oldItem in oldOrder.Items)
            {
                if (oldItem.Product == null || !oldItem.Product.IsAvailable)
                {
                    return BadRequest(new
                    {
                        Message = $"Product ID {oldItem.ProductId} is no longer available."
                    });
                }

                if (oldItem.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        Message = $"Invalid quantity for product ID {oldItem.ProductId}."
                    });
                }

                foreach (var selectedVariant in oldItem.SelectedVariants)
                {
                    if (selectedVariant.Variant == null ||
                        selectedVariant.Variant.ProductId != oldItem.ProductId)
                    {
                        return BadRequest(new
                        {
                            Message = $"A variant for product ID {oldItem.ProductId} is no longer valid."
                        });
                    }
                }
            }

            // 4. Option A: Add the old order's items to the existing cart.
            if (action.Equals("AddToCart", StringComparison.OrdinalIgnoreCase))
            {
                var cart = await _context.Carts
                    .Include(c => c.Items)
                        .ThenInclude(i => i.SelectedVariants)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                foreach (var oldItem in oldOrder.Items)
                {
                    var variantIds = oldItem.SelectedVariants
                        .Select(v => v.VariantId)
                        .OrderBy(id => id)
                        .ToList();

                    var existingItem = cart.Items.FirstOrDefault(i =>
                        i.ProductId == oldItem.ProductId &&
                        i.SelectedVariants
                            .Select(v => v.VariantId)
                            .OrderBy(id => id)
                            .SequenceEqual(variantIds));

                    if (existingItem != null)
                    {
                        existingItem.Quantity += oldItem.Quantity;
                    }
                    else
                    {
                        var newItem = new CartItem
                        {
                            CartId = cart.Id,
                            ProductId = oldItem.ProductId,
                            Quantity = oldItem.Quantity,
                            SelectedVariants = oldItem.SelectedVariants
                                .Select(v => new CartItemVariant
                                {
                                    VariantId = v.VariantId
                                })
                                .ToList()
                        };

                        _context.CartItems.Add(newItem);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Order items added to cart successfully."
                });
            }

            // 5. Option B: Checkout directly from the old order's items.
            // Use the old values when the customer leaves the fields empty.
            var addressId = dto.AddressId ?? oldOrder.AddressId;
            var paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod)
                ? oldOrder.PaymentMethod
                : dto.PaymentMethod.Trim();

            if (string.IsNullOrWhiteSpace(paymentMethod) ||
                (paymentMethod != "Cash" &&
                 paymentMethod != "Card" &&
                 paymentMethod != "NewCard" &&
                 paymentMethod != "SavedCard"))
            {
                return BadRequest(new
                {
                    Message = "Please choose a valid payment method."
                });
            }

            var address = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.Id == addressId && a.UserId == userId);

            if (address == null)
                return BadRequest(new { Message = "Invalid delivery address." });

            // If SavedCard is selected, use the provided card.
            // If omitted, fall back to the customer's default saved card.
            SavedCard? savedCard = null;

            if (paymentMethod == "SavedCard")
            {
                if (dto.SavedCardId.HasValue)
                {
                    savedCard = await _context.SavedCards.FirstOrDefaultAsync(c =>
                        c.Id == dto.SavedCardId.Value &&
                        c.UserId == userId);
                }
                else
                {
                    savedCard = await _context.SavedCards.FirstOrDefaultAsync(c =>
                        c.UserId == userId && c.IsDefault);
                }

                if (savedCard == null)
                {
                    return BadRequest(new
                    {
                        Message = "No suitable saved card found. Please choose a card."
                    });
                }
            }

            // 6. Recalculate prices using current product and variant prices.
            decimal subtotal = 0m;
            var newOrderItems = new List<OrderItem>();

            foreach (var oldItem in oldOrder.Items)
            {
                var productFinalPrice = Math.Round(
                    oldItem.Product.OriginalPrice *
                    (1m - oldItem.Product.DiscountPercent / 100m),
                    2,
                    MidpointRounding.AwayFromZero);

                var variantsPrice = oldItem.SelectedVariants
                    .Sum(v => v.Variant.ExtraPrice);

                var unitPrice = Math.Round(
                    productFinalPrice + variantsPrice,
                    2,
                    MidpointRounding.AwayFromZero);

                subtotal += unitPrice * oldItem.Quantity;

                newOrderItems.Add(new OrderItem
                {
                    ProductId = oldItem.ProductId,
                    Quantity = oldItem.Quantity,
                    UnitPriceSnapshot = unitPrice,
                    SelectedVariants = oldItem.SelectedVariants
                        .Select(v => new OrderItemVariant
                        {
                            VariantId = v.VariantId,
                            ExtraPriceSnapshot = v.Variant.ExtraPrice
                        })
                        .ToList()
                });
            }

            subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);

            // 7. Apply only a coupon provided for this new order.
            decimal discountAmount = 0m;
            YumQuick.Core.Entities.Coupon? appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var couponCode = dto.CouponCode.Trim();

                appliedCoupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                    c.Code == couponCode &&
                    c.IsActive &&
                    (c.ExpiryDate == null || c.ExpiryDate > DateTime.UtcNow) &&
                    c.CurrentUses < c.MaxUses);

                if (appliedCoupon == null)
                {
                    return BadRequest(new
                    {
                        Message = "Invalid or expired coupon."
                    });
                }

                discountAmount = Math.Round(
                    subtotal * appliedCoupon.DiscountPercentage / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            const decimal deliveryFee = 15.00m;

            var taxFee = Math.Round(
                (subtotal - discountAmount) * 0.14m,
                2,
                MidpointRounding.AwayFromZero);

            var totalAmount = Math.Round(
                subtotal - discountAmount + deliveryFee + taxFee,
                2,
                MidpointRounding.AwayFromZero);

            // 8. Create a completely new order.
            var newOrder = new Order
            {
                CustomerId = userId,
                AddressId = addressId,
                Status = OrderStatus.Pending,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                TaxFee = taxFee,
                CouponId = appliedCoupon?.Id,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Items = newOrderItems
            };

            if (appliedCoupon != null)
                appliedCoupon.CurrentUses++;

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            // Do not read from or clear the customer's existing cart.
            if (paymentMethod == "Cash")
            {
                return Ok(new
                {
                    Message = "Order placed successfully.",
                    OrderId = newOrder.Id
                });
            }

            // 9. Start card payment using the existing payment service.
            try
            {
                var clientSecret = await _paymentService.CreatePaymentIntentAsync(
                    newOrder,
                    paymentMethod == "SavedCard" ? savedCard!.Token : null);

                return Ok(new
                {
                    Message = paymentMethod == "SavedCard"
                        ? "Initiate Payment with Saved Card"
                        : "Initiate Payment",
                    OrderId = newOrder.Id,
                    ClientSecret = clientSecret
                });
            }
            catch (StripeException)
            {
                _context.Orders.Remove(newOrder);

                if (appliedCoupon != null)
                    appliedCoupon.CurrentUses--;

                await _context.SaveChangesAsync();

                return StatusCode(502, new
                {
                    Message = "Unable to initiate card payment. Please try again."
                });
            }
        }

    }
}