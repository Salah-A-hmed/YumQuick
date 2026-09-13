using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class ContactInfo
    {
        public int Id { get; set; }
        public string Platform { get; set; } // e.g. WhatsApp, Facebook
        public string UrlOrNumber { get; set; }
    }
}
