using System;
using System.Collections.Generic;
using System.Text;
using YumQuick.Core.Enums;

namespace YumQuick.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        public string DriverId { get; set; } // Nullable till driver accepts
        public ApplicationUser Driver { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TaxFee { get; set; }

        public int? CouponId { get; set; }
        public Coupon Coupon { get; set; }
        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public int? CancelReasonId { get; set; }
        public CancelReason CancelReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }

        public ICollection<OrderItem> Items { get; set; }
    }
}
