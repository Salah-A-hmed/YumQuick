using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YumQuick.Core.DTOs;
using YumQuick.Core.Enums;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "DeliveryDriver")]
    public class DriversController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DriversController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Drivers/available-orders
        [HttpGet("available-orders")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Where(o => o.Status == OrderStatus.ReadyForDelivery && o.DriverId == null)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.DeliveryFee,
                    o.CreatedAt,
                    DeliveryAddress = new { o.Address.Label, o.Address.FullAddress, o.Address.Latitude, o.Address.Longitude }
                })
                .ToListAsync();

            return Ok(orders);
        }

        // POST: api/Drivers/orders/{id}/accept
        [HttpPost("orders/{id}/accept")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { Message = "Order not found." });

            if (order.Status != OrderStatus.ReadyForDelivery || order.DriverId != null)
                return BadRequest(new { Message = "Order is no longer available for delivery." });

            // ربط الطلب بالمندوب الحالي
            order.DriverId = driverId;
            order.Status = OrderStatus.OnTheWay;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order accepted successfully.", OrderId = order.Id });
        }

        // POST: api/Drivers/orders/{id}/deliver
        [HttpPost("orders/{id}/deliver")]
        public async Task<IActionResult> MarkOrderAsDelivered(int id)
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.DriverId == driverId);

            if (order == null) return NotFound(new { Message = "Order not found or not assigned to you." });

            if (order.Status != OrderStatus.OnTheWay)
            {
                return BadRequest(new { Message = "Order must be 'OnTheWay' before it can be marked as delivered." });
            }

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order delivered successfully." });
        }

        // GET: api/Drivers/earnings
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings()
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.UtcNow.Date;

            var completedOrders = await _context.Orders
                .Where(o => o.DriverId == driverId && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var totalOrdersDelivered = completedOrders.Count;
            var totalEarnings = completedOrders.Sum(o => o.DeliveryFee);

            var todayEarnings = completedOrders
                .Where(o => o.DeliveredAt.HasValue && o.DeliveredAt.Value.Date == today)
                .Sum(o => o.DeliveryFee);

            return Ok(new
            {
                TotalOrdersDelivered = totalOrdersDelivered,
                TotalEarnings = totalEarnings,
                TodayEarnings = todayEarnings
            });
        }
    }
}