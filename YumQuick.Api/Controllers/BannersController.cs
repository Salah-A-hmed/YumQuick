using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumQuick.Core.Entities;
using YumQuick.Core.Interfaces;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public BannersController(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }


        [HttpGet]
        public async Task<IActionResult> GetActiveBanners()
        {
            var banners = await _context.Banners
                .Where(b => b.IsActive)
                .Select(b => new { b.Id, b.ImageUrl, b.Title })
                .ToListAsync();

            return Ok(banners);
        }

        [HttpPost]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> CreateBanner([FromForm] IFormFile image, [FromForm] string? title)
        {
            if (image == null || image.Length == 0) return BadRequest("Image is required");

            var imageUrl = await _imageService.UploadImageAsync(image);

            var banner = new Banner
            {
                ImageUrl = imageUrl,
                Title = title,
                IsActive = true
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Banner added successfully", BannerId = banner.Id });
        }
    }
}