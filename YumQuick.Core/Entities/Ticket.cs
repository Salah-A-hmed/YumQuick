using System;
using System.Collections.Generic;
using YumQuick.Core.Enums;

namespace YumQuick.Core.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int? OrderId { get; set; }
        public Order Order { get; set; }

        public string Subject { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    }
}