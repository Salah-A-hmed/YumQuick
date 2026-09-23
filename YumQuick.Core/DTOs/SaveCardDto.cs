using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class SaveCardDto
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string LastFourDigits { get; set; }
        [Required]
        public string Brand { get; set; }
    }

}