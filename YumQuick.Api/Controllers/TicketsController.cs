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
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Tickets
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Tickets.AsQueryable();

            if (userRole != "RestaurantManager")
            {
                query = query.Where(t => t.UserId == userId);
            }

            var tickets = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Subject,
                    t.Status,
                    t.OrderId,
                    t.CreatedAt,
                    t.UpdatedAt,
                    MessageCount = t.Messages.Count
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // GET: api/Tickets/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var ticket = await _context.Tickets
                .Include(t => t.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound("Ticket not found.");

            if (userRole != "RestaurantManager" && ticket.UserId != userId)
                return Forbid();

            var result = new
            {
                ticket.Id,
                ticket.Subject,
                ticket.Status,
                ticket.OrderId,
                ticket.CreatedAt,
                Messages = ticket.Messages.OrderBy(m => m.CreatedAt).Select(m => new
                {
                    m.Id,
                    m.Message,
                    m.CreatedAt,
                    SenderName = m.Sender.FullName,
                    IsAdmin = m.SenderId != ticket.UserId
                })
            };

            return Ok(result);
        }

        // POST: api/Tickets
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.OrderId.HasValue)
            {
                var orderExists = await _context.Orders.AnyAsync(o => o.Id == dto.OrderId.Value && o.CustomerId == userId);
                if (!orderExists) return BadRequest("Invalid Order ID.");
            }

            var ticket = new Ticket
            {
                UserId = userId,
                Subject = dto.Subject,
                OrderId = dto.OrderId,
                Messages = new List<TicketMessage>
                {
                    new TicketMessage
                    {
                        SenderId = userId,
                        Message = dto.InitialMessage
                    }
                }
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ticket created successfully.", TicketId = ticket.Id });
        }

        // POST: api/Tickets/{id}/reply
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToTicket(int id, [FromBody] ReplyTicketDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound("Ticket not found.");

            if (userRole != "RestaurantManager" && ticket.UserId != userId)
                return Forbid();

            if (ticket.Status == TicketStatus.Closed)
                return BadRequest("Cannot reply to a closed ticket.");

            var message = new TicketMessage
            {
                TicketId = id,
                SenderId = userId,
                Message = dto.Message
            };

            _context.TicketMessages.Add(message);

            ticket.UpdatedAt = DateTime.UtcNow;
            if (userRole == "RestaurantManager") ticket.Status = TicketStatus.InProgress;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Reply sent successfully." });
        }

        // PUT: api/Tickets/{id}/close
        [HttpPut("{id}/close")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound("Ticket not found.");

            ticket.Status = TicketStatus.Closed;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ticket closed successfully." });
        }
    }
}