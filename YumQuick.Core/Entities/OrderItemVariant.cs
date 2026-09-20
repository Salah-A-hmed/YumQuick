namespace YumQuick.Core.Entities
{
    public class OrderItemVariant
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; }

        public int VariantId { get; set; }
        public ProductVariant Variant { get; set; }

        public decimal ExtraPriceSnapshot { get; set; }
    }
}