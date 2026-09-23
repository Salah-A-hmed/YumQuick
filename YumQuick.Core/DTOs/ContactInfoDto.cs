using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace YumQuick.Core.DTOs
{
    public class ContactInfoDto
    {
        [Required]
        public string Platform { get; set; }
        [Required]
        public string UrlOrNumber { get; set; }
    }
}
