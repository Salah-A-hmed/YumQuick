using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CheckoutDto
    {
        [Required]
        public int AddressId { get; set; }

        public string? CouponCode { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "Cash", "NewCard", "SavedCard"

        public int? SavedCardId { get; set; } // سيتم استخدامه إذا كان PaymentMethod = "SavedCard"
    }
}