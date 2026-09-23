using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsBestSeller { get; set; }
        public bool? IsNew { get; set; }
        public int? CategoryId { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class UpdateVariantDto
    {
        public string? Name { get; set; }
        public decimal? ExtraPrice { get; set; }
    }
}