using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "RestaurantManager")]
    public class CouponsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Coupons
        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return Ok(coupons);
        }

        // POST: api/Coupons
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] CouponDto dto)
        {
            var codeExists = await _context.Coupons.AnyAsync(c => c.Code.ToLower() == dto.Code.ToLower());
            if (codeExists) return BadRequest("Coupon code already exists.");

            var coupon = new Coupon
            {
                Code = dto.Code.ToUpper(),
                DiscountPercentage = dto.DiscountPercentage,
                MaxUses = dto.MaxUses,
                ExpiryDate = dto.ExpiryDate,
                IsActive = dto.IsActive
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Coupon created successfully", CouponId = coupon.Id });
        }

        // PUT: api/Coupons/{id}/toggle-status
        [HttpPut("{id}/toggle-status")]
        public async Task<IActionResult> ToggleCouponStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound("Coupon not found.");

            coupon.IsActive = !coupon.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Coupon status updated to {coupon.IsActive}" });
        }

        // DELETE: api/Coupons/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound("Coupon not found.");

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Coupon deleted successfully" });
        }
    }
}