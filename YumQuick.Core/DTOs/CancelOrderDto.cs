using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace YumQuick.Core.DTOs
{
    public class CancelOrderDto
    {
        public int? CancelReasonId { get; set; }
        public string? CustomCancelReason { get; set; }
    }
}
