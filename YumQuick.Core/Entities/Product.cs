using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; } // e.g. 15 for 15%
        public decimal RatingAvg { get; set; } = 0;

        public bool IsAvailable { get; set; } = true;
        public bool IsBestSeller { get; set; } = false;
        public bool IsNew { get; set; } = true;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<ProductVariant> Variants { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
