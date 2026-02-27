using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Domain.Enum
{
    public enum BookingStatusEnum
    {
        Pending = 0,
        Confirmed = 1,
        Rejected = 2,
        Cancelled = 3,
        Completed = 4
    }
}
