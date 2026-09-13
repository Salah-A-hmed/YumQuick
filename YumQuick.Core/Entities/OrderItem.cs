using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int? VariantId { get; set; }
        public ProductVariant Variant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; } // السعر وقت الطلب
    }
}
