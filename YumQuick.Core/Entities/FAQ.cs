using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class FAQ
    {
        public int Id { get; set; }
        public string Category { get; set; } // General, Account, Services
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}
