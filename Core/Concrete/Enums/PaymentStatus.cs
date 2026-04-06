using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Concrete.Enums
{
    public enum PaymentStatus
    {
        Pending=1,
        Completed=2,
        Failed=3,
        Refunded=4,
        PartiallyRefunded = 5
    };
}
