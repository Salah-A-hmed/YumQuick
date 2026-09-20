using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CreateReviewDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}