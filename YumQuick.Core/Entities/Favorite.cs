using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class Favorite
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
