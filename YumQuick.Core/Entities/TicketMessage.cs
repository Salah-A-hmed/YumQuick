using System;

namespace YumQuick.Core.Entities
{
    public class TicketMessage
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}