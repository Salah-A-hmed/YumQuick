using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumQuick.Core.Enums;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "RestaurantManager")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.UtcNow.Date;

            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            var canceledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount);

            var todayRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.CreatedAt >= today)
                .SumAsync(o => o.TotalAmount);

            var topProducts = await _context.OrderItems
                .Include(i => i.Product)
                .GroupBy(i => new { i.ProductId, i.Product.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                CanceledOrders = canceledOrders,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                TopSellingProducts = topProducts
            });
        }
    }
}