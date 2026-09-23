using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.DTOs
{
    public class SavedCardResponseDto
    {
        public int Id { get; set; }
        public string LastFourDigits { get; set; }
        public string Brand { get; set; }
        public bool IsDefault { get; set; }
    }
}
