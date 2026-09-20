using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CheckoutDto
    {
        [Required]
        public int AddressId { get; set; }

        public string? CouponCode { get; set; }
    }
}