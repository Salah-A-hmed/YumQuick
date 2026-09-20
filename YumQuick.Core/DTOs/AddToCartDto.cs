using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "Quantity must be between 1 and 50")]
        public int Quantity { get; set; }

        public List<int>? VariantIds { get; set; }
    }
}