namespace YumQuick.Core.Entities
{
    public class CartItemVariant
    {
        public int Id { get; set; }
        public int CartItemId { get; set; }
        public CartItem CartItem { get; set; }

        public int VariantId { get; set; }
        public ProductVariant Variant { get; set; }
    }
}