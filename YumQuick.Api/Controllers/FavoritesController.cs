using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YumQuick.Core.Entities;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Favorites/toggle/{productId}
        [HttpPost("toggle/{productId}")]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists) return NotFound(new { Message = "Product not found" });

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existingFavorite != null)
            {
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Removed from favorites", IsFavorite = false });
            }
            else
            {
                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Added to favorites", IsFavorite = true });
            }
        }

        // GET: api/Favorites
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .Select(f => new
                {
                    f.Product.Id,
                    f.Product.Name,
                    f.Product.Description,
                    f.Product.ImageUrl,
                    f.Product.OriginalPrice,
                    f.Product.DiscountPercent,
                    FinalPrice = f.Product.OriginalPrice - (f.Product.OriginalPrice * (f.Product.DiscountPercent / 100m)),
                    f.Product.RatingAvg,
                    f.Product.IsAvailable
                }).ToListAsync();

            return Ok(favorites);
        }
    }
}