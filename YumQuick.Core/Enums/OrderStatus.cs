using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Enums
{
    public enum OrderStatus
    {
        Pending,
        Preparing,
        ReadyForDelivery,
        OnTheWay,
        Delivered,
        Cancelled
    }
}
