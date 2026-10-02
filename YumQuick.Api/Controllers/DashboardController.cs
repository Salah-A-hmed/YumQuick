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
            var activeCustomers = await _context.Orders
                .Select(o => o.CustomerId)
                .Distinct()
                .CountAsync();

            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);

            var deliveredOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Delivered);

            var canceledOrders = await _context.Orders.
                CountAsync(o => o.Status == OrderStatus.Cancelled);

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

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
                ActiveCustomers = activeCustomers,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                CanceledOrders = canceledOrders,
                TotalRevenue = totalRevenue,
                TopSellingProducts = topProducts
            });
        }

        // GET: api/Dashboard/summary/period?period=today
        [HttpGet("summary/period")]
        public async Task<IActionResult> GetPeriodSummary(
        [FromQuery] string period = "today",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        {
            var now = DateTime.UtcNow;

            DateTime startDate;
            DateTime endDate;

            switch (period.ToLowerInvariant())
            {
                case "today":
                    startDate = now.Date;
                    endDate = startDate.AddDays(1);
                    break;

                case "week":
                    // Week starts on Sunday
                    int daysSinceSunday = (int)now.DayOfWeek;
                    startDate = now.Date.AddDays(-daysSinceSunday);
                    endDate = startDate.AddDays(7);
                    break;

                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;

                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;

                case "custom":
                    if (!from.HasValue || !to.HasValue)
                    {
                        return BadRequest(new
                        {
                            Message = "Both 'from' and 'to' dates are required."
                        });
                    }

                    if (from.Value.Date > to.Value.Date)
                    {
                        return BadRequest(new
                        {
                            Message = "'from' date must be before or equal to 'to' date."
                        });
                    }

                    startDate = from.Value.Date;
                    endDate = to.Value.Date.AddDays(1);
                    break;

                default:
                    return BadRequest(new
                    {
                        Message = "Invalid period. Use today, week, month, year, or custom."
                    });
            }

            var orders = _context.Orders
                .Where(o =>
                    o.CreatedAt >= startDate &&
                    o.CreatedAt < endDate);

            var totalOrders = await orders.CountAsync();

            var pendingOrders = await orders
                .CountAsync(o => o.Status == OrderStatus.Pending);

            var deliveredOrders = await orders
                .CountAsync(o => o.Status == OrderStatus.Delivered);

            var canceledOrders = await orders
                .CountAsync(o => o.Status == OrderStatus.Cancelled);

            var totalRevenue = await orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var activeCustomers = await orders
                .Select(o => o.CustomerId)
                .Distinct()
                .CountAsync();

            var topProducts = await _context.OrderItems
                .Where(i =>
                    i.Order.CreatedAt >= startDate &&
                    i.Order.CreatedAt < endDate)
                .GroupBy(i => new
                {
                    i.ProductId,
                    i.Product.Name
                })
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
                Period = period,
                From = startDate,
                To = endDate.AddTicks(-1),

                ActiveCustomers = activeCustomers,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                CanceledOrders = canceledOrders,
                TotalRevenue = totalRevenue,
                TopSellingProducts = topProducts
            });
        }

        // GET: api/Dashboard/revenue
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueChart(
            [FromQuery] string period = "week")
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var paidOrders = _context.Orders
                .Where(o =>
                    o.PaymentStatus == PaymentStatus.Paid ||
                    o.Status == OrderStatus.Delivered);

            var revenueData = new List<object>();

            switch (period.ToLowerInvariant())
            {
                case "week":
                    {
                        // Current week: Sunday to Saturday
                        int daysSinceSunday = (int)today.DayOfWeek;
                        var startDate = today.AddDays(-daysSinceSunday);
                        var endDate = startDate.AddDays(7);

                        var data = await paidOrders
                            .Where(o =>
                                o.CreatedAt >= startDate &&
                                o.CreatedAt < endDate)
                            .GroupBy(o => o.CreatedAt.Date)
                            .Select(g => new
                            {
                                Date = g.Key,
                                Revenue = g.Sum(o => o.TotalAmount),
                                OrderCount = g.Count()
                            })
                            .ToListAsync();

                        for (int i = 0; i < 7; i++)
                        {
                            var date = startDate.AddDays(i);
                            var item = data.FirstOrDefault(x => x.Date == date);

                            revenueData.Add(new
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Day = date.ToString("ddd"),
                                Revenue = item?.Revenue ?? 0m,
                                OrderCount = item?.OrderCount ?? 0
                            });
                        }

                        break;
                    }

                case "month":
                    {
                        // Current month, grouped by Sunday-based weeks
                        var startDate = new DateTime(now.Year, now.Month, 1);
                        var endDate = startDate.AddMonths(1);

                        // SQL Server DATEDIFF(WEEK) uses Sunday boundaries.
                        var sundayAnchor = new DateTime(1900, 1, 7);

                        var data = await paidOrders
                            .Where(o =>
                                o.CreatedAt >= startDate &&
                                o.CreatedAt < endDate)
                            .GroupBy(o =>
                                EF.Functions.DateDiffWeek(
                                    sundayAnchor, o.CreatedAt))
                            .Select(g => new
                            {
                                WeekIndex = g.Key,
                                Revenue = g.Sum(o => o.TotalAmount),
                                OrderCount = g.Count()
                            })
                            .ToListAsync();

                        var firstSunday = startDate.AddDays(
                            -(int)startDate.DayOfWeek);

                        var lastSunday = endDate.AddDays(
                            -(int)endDate.DayOfWeek);

                        for (var sunday = firstSunday;
                             sunday < endDate;
                             sunday = sunday.AddDays(7))
                        {
                            var weekIndex =
                                EF.Functions.DateDiffWeek(
                                    sundayAnchor, sunday);

                            var item = data.FirstOrDefault(
                                x => x.WeekIndex == weekIndex);

                            revenueData.Add(new
                            {
                                Date = sunday.ToString("yyyy-MM-dd"),
                                Week = $"Week {(sunday - firstSunday).Days / 7 + 1}",
                                Revenue = item?.Revenue ?? 0m,
                                OrderCount = item?.OrderCount ?? 0
                            });
                        }

                        break;
                    }

                case "year":
                    {
                        // Current year, grouped by quarters
                        var startDate = new DateTime(now.Year, 1, 1);
                        var endDate = startDate.AddYears(1);

                        var data = await paidOrders
                            .Where(o =>
                                o.CreatedAt >= startDate &&
                                o.CreatedAt < endDate)
                            .GroupBy(o => (o.CreatedAt.Month - 1) / 3 + 1)
                            .Select(g => new
                            {
                                Quarter = g.Key,
                                Revenue = g.Sum(o => o.TotalAmount),
                                OrderCount = g.Count()
                            })
                            .ToListAsync();

                        for (int quarter = 1; quarter <= 4; quarter++)
                        {
                            var item = data.FirstOrDefault(
                                x => x.Quarter == quarter);

                            revenueData.Add(new
                            {
                                Year = now.Year,
                                Quarter = $"Q{quarter}",
                                Revenue = item?.Revenue ?? 0m,
                                OrderCount = item?.OrderCount ?? 0
                            });
                        }

                        break;
                    }

                case "all":
                    {
                        // All time, grouped by year
                        var data = await paidOrders
                            .GroupBy(o => o.CreatedAt.Year)
                            .Select(g => new
                            {
                                Year = g.Key,
                                Revenue = g.Sum(o => o.TotalAmount),
                                OrderCount = g.Count()
                            })
                            .OrderBy(x => x.Year)
                            .ToListAsync();

                        revenueData = data
                            .Select(x => (object)new
                            {
                                Year = x.Year,
                                Revenue = x.Revenue,
                                OrderCount = x.OrderCount
                            })
                            .ToList();

                        break;
                    }

                default:
                    return BadRequest(new
                    {
                        Message = "Invalid period. Use week, month, year, or all."
                    });
            }

            return Ok(new
            {
                Period = period.ToLowerInvariant(),
                Data = revenueData
            });
        }

        // GET: api/Dashboard/drivers-performance

        [HttpGet("drivers-performance")]
        public async Task<IActionResult> GetDriversPerformance(
            [FromQuery] string period = "all")
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;

            switch (period.ToLowerInvariant())
            {
                case "day":
                    startDate = today;
                    endDate = today.AddDays(1);
                    break;

                case "week":
                    // Week starts on Sunday
                    int daysSinceSunday = (int)today.DayOfWeek;
                    startDate = today.AddDays(-daysSinceSunday);
                    endDate = startDate.AddDays(7);
                    break;

                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;

                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;

                case "all":
                    // No date filter
                    break;

                default:
                    return BadRequest(new
                    {
                        Message = "Invalid period. Use day, week, month, year, or all."
                    });
            }

            var orders = _context.Orders
                .Where(o =>
                    o.DriverId != null &&
                    o.Status == OrderStatus.Delivered);

            if (period.ToLowerInvariant() != "all")
            {
                orders = orders.Where(o =>
                    o.CreatedAt >= startDate &&
                    o.CreatedAt < endDate);
            }

            var performance = await orders
                .GroupBy(o => new
                {
                    o.DriverId,
                    DriverName = o.Driver.FullName
                })
                .Select(g => new
                {
                    DriverId = g.Key.DriverId,
                    DriverName = g.Key.DriverName,
                    TotalDeliveredOrders = g.Count(),
                    TotalCashCollected = g
                        .Where(o => o.PaymentMethod == "Cash")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .OrderByDescending(d => d.TotalDeliveredOrders)
                .ToListAsync();

            return Ok(new
            {
                Period = period.ToLowerInvariant(),
                Drivers = performance
            });
        }
    }
}