using System.ComponentModel.DataAnnotations;

namespace YumQuick.Core.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        public string Subject { get; set; }
        [Required]
        public string InitialMessage { get; set; }
        public int? OrderId { get; set; }
    }
}