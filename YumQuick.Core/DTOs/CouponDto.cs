using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CouponDto
    {
        [Required]
        public string Code { get; set; }

        [Required]
        [Range(1, 100)]
        public decimal DiscountPercentage { get; set; }

        [Required]
        [Range(1, 100000)]
        public int MaxUses { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}