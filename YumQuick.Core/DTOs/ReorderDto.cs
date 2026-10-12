
namespace YumQuick.Core.DTOs
{
    public class ReorderDto
    {
        public string? Action { get; set; } // AddToCart or Checkout

        public int? AddressId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CouponCode { get; set; }
        public int? SavedCardId { get; set; }
    }
}
