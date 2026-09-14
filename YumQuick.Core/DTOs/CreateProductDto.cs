using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CreateProductDto
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        [Required]
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }

        public bool IsAvailable { get; set; } = true;
        public bool IsBestSeller { get; set; } = false;
        public bool IsNew { get; set; } = true;

        [Required]
        public int CategoryId { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class CreateVariantDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public decimal ExtraPrice { get; set; }
    }
}