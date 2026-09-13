using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int MaxUses { get; set; }
        public int CurrentUses { get; set; } = 0;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;

        [Timestamp] // Concurrency Token
        public byte[] RowVersion { get; set; }
    }
}
