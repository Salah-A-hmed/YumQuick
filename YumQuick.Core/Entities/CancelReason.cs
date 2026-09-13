using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Entities
{
    public class CancelReason
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
