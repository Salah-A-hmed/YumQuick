using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class Banner
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public int? CategoryId { get; set; } // Optional link to category
        public int? ProductId { get; set; }  // Optional link to product
        public bool IsActive { get; set; } = true;
    }
}
