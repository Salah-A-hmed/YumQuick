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
    public class PaymentMethodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentMethodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/PaymentMethods
        [HttpGet]
        public async Task<IActionResult> GetMyCards()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cards = await _context.SavedCards
                .Where(c => c.UserId == userId)
                .Select(c => new SavedCardResponseDto
                {
                    Id = c.Id,
                    LastFourDigits = c.LastFourDigits,
                    Brand = c.Brand,
                    IsDefault = c.IsDefault
                })
                .ToListAsync();

            return Ok(cards);
        }

        // POST: api/PaymentMethods
        // بيتم استدعاؤها بعد ما الموبايل ينجح في حفظ البطاقة مع Fawry SDK
        [HttpPost]
        public async Task<IActionResult> AddCard([FromBody] SaveCardDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // لو دي أول بطاقة، خليها الافتراضية
            var hasCards = await _context.SavedCards.AnyAsync(c => c.UserId == userId);

            var savedCard = new SavedCard
            {
                UserId = userId,
                Token = dto.Token,
                LastFourDigits = dto.LastFourDigits,
                Brand = dto.Brand,
                IsDefault = !hasCards
            };

            _context.SavedCards.Add(savedCard);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Card saved successfully.", CardId = savedCard.Id });
        }

        // PUT: api/PaymentMethods/{id}/set-default
        [HttpPut("{id}/set-default")]
        public async Task<IActionResult> SetDefaultCard(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cards = await _context.SavedCards.Where(c => c.UserId == userId).ToListAsync();
            var targetCard = cards.FirstOrDefault(c => c.Id == id);

            if (targetCard == null) return NotFound("Card not found.");

            foreach (var card in cards)
            {
                card.IsDefault = (card.Id == id);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Default card updated." });
        }

        // DELETE: api/PaymentMethods/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var card = await _context.SavedCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (card == null) return NotFound("Card not found.");

            _context.SavedCards.Remove(card);

            // لو مسح البطاقة الافتراضية، خلي أول بطاقة تانية هي الافتراضية
            if (card.IsDefault)
            {
                var nextCard = await _context.SavedCards.FirstOrDefaultAsync(c => c.UserId == userId && c.Id != id);
                if (nextCard != null) nextCard.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Card deleted successfully." });
        }
    }
}