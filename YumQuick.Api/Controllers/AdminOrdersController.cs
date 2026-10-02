using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumQuick.Core.Enums;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/Admin/Orders")]
    [ApiController]
    [Authorize(Roles = "RestaurantManager")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Admin/Orders
        [HttpGet]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] string? status,
            [FromQuery] string? paymentMethod,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse(typeof(OrderStatus), status, true, out var parsedStatus))
            {
                query = query.Where(o => o.Status == (OrderStatus)parsedStatus);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(o => o.PaymentMethod == paymentMethod);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= toDate.Value);
            }

            var orders = await query
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.CustomerId,
                    o.Customer.FullName,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Items.Count,
                    o.TotalAmount,
                    o.CreatedAt,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            return Ok(orders);
        }
        // GET: api/Admin/Orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.SelectedVariants)
                        .ThenInclude(v => v.Variant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Order not found.");


            var result = new
            {
                order.Id,
                order.CustomerId,
                CustomerName = order.Customer.FullName,
                DriverId = order.DriverId,
                DriverName = order.Driver?.FullName,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.Subtotal,
                order.DeliveryFee,
                order.TaxFee,
                order.DiscountAmount,
                order.TotalAmount,
                order.CreatedAt,
                order.DeliveredAt,
                DeliveryAddress = order.Address?.FullAddress,
                Items = order.Items.Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product.Name,
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

        // POST: api/Admin/Orders/{id}/prepare
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

        // POST: api/Admin/Orders/{id}/ready
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

        // POST: api/Admin/Orders/{id}/assign

        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignOrderToDriver(int id, [FromBody] AssignDriverDto dto)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Order not found.");

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                return BadRequest("Cannot assign a delivered or cancelled order.");

            if (string.IsNullOrEmpty(dto.DriverId))
                return BadRequest("DriverId is required.");

            order.DriverId = dto.DriverId;

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Preparing || order.Status == OrderStatus.ReadyForDelivery)
            {
                order.Status = OrderStatus.OnTheWay;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order assigned to driver successfully." });
        }

    }

    public class AssignDriverDto
    {
        public string DriverId { get; set; } = string.Empty;
    }
}