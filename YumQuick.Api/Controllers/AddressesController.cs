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
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Addresses
        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new
                {
                    a.Id,
                    a.Label,
                    a.FullAddress,
                    a.Latitude,
                    a.Longitude,
                    a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        // POST: api/Addresses
        [HttpPost]
        public async Task<IActionResult> AddAddress([FromBody] CreateAddressDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isFirstAddress = !await _context.Addresses.AnyAsync(a => a.UserId == userId);

            var address = new Address
            {
                UserId = userId,
                Label = dto.Label,
                FullAddress = dto.FullAddress,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsDefault = isFirstAddress
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Address added successfully", AddressId = address.Id });
        }

        // PUT: api/Addresses/set-default/{id}
        [HttpPut("set-default/{id}")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null) return NotFound(new { Message = "Address not found" });

            var oldDefaults = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
            foreach (var old in oldDefaults)
            {
                old.IsDefault = false;
            }

            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Default address updated" });
        }

        // DELETE: api/Addresses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null) return NotFound(new { Message = "Address not found" });

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Address deleted successfully" });
        }
    }
}