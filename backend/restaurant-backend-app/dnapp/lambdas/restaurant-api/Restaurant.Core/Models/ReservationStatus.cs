using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Models
{
    public enum ReservationStatus
    {
        Reserved = 1,
        Canceled = 2,
        InProgress = 3,
        Finished = 4
    }
}
