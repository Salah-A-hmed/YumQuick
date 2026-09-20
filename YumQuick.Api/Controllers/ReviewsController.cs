using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Core.Enums;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. التأكد إن الأوردر موجود وبتاع العميل ده، وحالته Delivered
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.CustomerId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            if (order.Status != OrderStatus.Delivered)
                return BadRequest(new { Message = "You can only review delivered orders." });

            // 2. التأكد إن المنتج ده فعلاً كان جوه الأوردر ده
            if (!order.Items.Any(i => i.ProductId == dto.ProductId))
                return BadRequest(new { Message = "This product is not in the specified order." });

            // 3. التأكد إن العميل مقيمش المنتج ده في الأوردر ده قبل كده (منع التكرار)
            var existingReview = await _context.Reviews
                .AnyAsync(r => r.OrderId == dto.OrderId && r.ProductId == dto.ProductId && r.UserId == userId);

            if (existingReview)
                return BadRequest(new { Message = "You have already reviewed this product for this order." });

            // 4. حفظ التقييم بناءً على الكلاس بتاعك
            var review = new Review
            {
                UserId = userId,
                OrderId = dto.OrderId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // 5. تحديث متوسط التقييم (RatingAvg) للمنتج
            var productReviews = await _context.Reviews
                .Where(r => r.ProductId == dto.ProductId)
                .ToListAsync();

            var newAverage = productReviews.Average(r => r.Rating);

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product != null)
            {
                product.RatingAvg = (decimal)newAverage;
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Review submitted successfully." });
        }
    }
}