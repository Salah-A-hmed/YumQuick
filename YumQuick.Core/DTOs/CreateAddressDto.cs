using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CreateAddressDto
    {
        [Required]
        public string Label { get; set; }

        [Required]
        public string FullAddress { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}