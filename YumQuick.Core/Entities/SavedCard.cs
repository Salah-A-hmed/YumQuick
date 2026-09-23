using System;

namespace YumQuick.Core.Entities
{
    public class SavedCard
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Token { get; set; } // الرمز المشفر من فوري
        public string LastFourDigits { get; set; } // مثلاً 4242
        public string Brand { get; set; } // Visa, Mastercard
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}