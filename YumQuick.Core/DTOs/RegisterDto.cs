using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YumQuick.Core.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        public string? Role { get; set; }

        public IFormFile? Avatar { get; set; }
    }
}