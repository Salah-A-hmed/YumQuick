using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Label { get; set; }
        public string FullAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}
