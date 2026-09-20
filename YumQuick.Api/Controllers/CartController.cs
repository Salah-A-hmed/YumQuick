using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(sv => sv.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
            {
                return Ok(new { Message = "Cart is empty", Items = new List<object>(), TotalCartPrice = 0 });
            }

            decimal totalCartPrice = 0;

            var itemsResponse = cart.Items.Select(i =>
            {
                var productFinalPrice = i.Product.OriginalPrice - (i.Product.OriginalPrice * (i.Product.DiscountPercent / 100m));
                var variantsPrice = i.SelectedVariants.Sum(v => v.Variant.ExtraPrice);

                var unitPrice = productFinalPrice + variantsPrice;
                var totalItemPrice = unitPrice * i.Quantity;

                totalCartPrice += totalItemPrice;

                return new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product.Name,
                    ProductImage = i.Product.ImageUrl,
                    i.Quantity,
                    UnitPrice = unitPrice,
                    TotalItemPrice = totalItemPrice,
                    SelectedVariants = i.SelectedVariants.Select(v => new
                    {
                        v.Variant.Id,
                        v.Variant.Name,
                        v.Variant.ExtraPrice
                    })
                };
            });

            return Ok(new { Items = itemsResponse, TotalCartPrice = totalCartPrice });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.IsAvailable);
            if (product == null) return NotFound(new { Message = "Product not found or not available" });

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

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == dto.ProductId &&
                AreVariantsEqual(i.SelectedVariants.Select(v => v.VariantId).ToList(), dto.VariantIds ?? new List<int>())
            );

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };

                if (dto.VariantIds != null && dto.VariantIds.Any())
                {
                    var validVariantIds = await _context.ProductVariants
                        .Where(v => v.ProductId == dto.ProductId && dto.VariantIds.Contains(v.Id))
                        .Select(v => v.Id)
                        .ToListAsync();

                    foreach (var varId in validVariantIds)
                    {
                        newItem.SelectedVariants.Add(new CartItemVariant { VariantId = varId });
                    }
                }

                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Added to cart successfully" });
        }

        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            var item = await _context.CartItems.FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id);
            if (item == null) return NotFound("Item not found in cart");

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item removed successfully" });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return Ok(new { Message = "Cart is already empty" });

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cart cleared successfully" });
        }

        private bool AreVariantsEqual(List<int> list1, List<int> list2)
        {
            if (list1.Count != list2.Count) return false;
            var set1 = new HashSet<int>(list1);
            return set1.SetEquals(list2);
        }
    }
}