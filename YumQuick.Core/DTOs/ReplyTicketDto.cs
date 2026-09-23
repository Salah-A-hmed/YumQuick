using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace YumQuick.Core.DTOs
{
    public class ReplyTicketDto
    {
        [Required]
        public string Message { get; set; }
    }
}
