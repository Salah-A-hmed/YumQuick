using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        public string Name { get; set; }

        public IFormFile? Icon { get; set; }

        public int? ParentCategoryId { get; set; }
    }
}