using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Concrete.Enums
{
    public enum TransactionType
    {
        Sale = 1,
        Auth=2,
        Capture = 3,
        Void = 4,
        Refund = 5
    };
}
