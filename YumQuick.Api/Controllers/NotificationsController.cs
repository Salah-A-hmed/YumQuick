using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using YumQuick.Core.Entities;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Notifications
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // PUT: api/Notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return NotFound("Notification not found.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read." });
        }

        // POST: api/Notifications/save-token
        [HttpPost("save-token")]
        public async Task<IActionResult> SaveDeviceToken([FromBody] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // التحقق مما إذا كان التوكن مسجلاً مسبقاً لهذا المستخدم لتجنب التكرار
            var tokenExists = await _context.DeviceTokens.AnyAsync(t => t.UserId == userId && t.Token == token);

            if (!tokenExists)
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId,
                    Token = token
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Device token saved successfully." });
        }
    }
}