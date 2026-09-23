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
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= FAQs =================

        [HttpGet("faqs")]
        public async Task<IActionResult> GetFAQs()
        {
            return Ok(await _context.FAQs.ToListAsync());
        }

        [HttpPost("faqs")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> AddFAQ([FromBody] FAQDto dto)
        {
            var faq = new FAQ { Category = dto.Category, Question = dto.Question, Answer = dto.Answer };
            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "FAQ added successfully", Id = faq.Id });
        }

        [HttpDelete("faqs/{id}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound("FAQ not found");

            _context.FAQs.Remove(faq);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "FAQ deleted successfully" });
        }

        // ================= Contact Info =================

        [HttpGet("contact-info")]
        public async Task<IActionResult> GetContactInfo()
        {
            return Ok(await _context.ContactInfos.ToListAsync());
        }

        [HttpPost("contact-info")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> AddContactInfo([FromBody] ContactInfoDto dto)
        {
            var contact = new ContactInfo { Platform = dto.Platform, UrlOrNumber = dto.UrlOrNumber };
            _context.ContactInfos.Add(contact);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Contact info added successfully", Id = contact.Id });
        }

        [HttpDelete("contact-info/{id}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> DeleteContactInfo(int id)
        {
            var contact = await _context.ContactInfos.FindAsync(id);
            if (contact == null) return NotFound("Contact info not found");

            _context.ContactInfos.Remove(contact);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Contact info deleted successfully" });
        }
    }
}