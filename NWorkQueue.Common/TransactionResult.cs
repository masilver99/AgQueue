using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Common
{
    // This may need to be in the public API
    public enum TransactionResult
    {
        Success = 0,
        Expired = 1,
        Closed = 2,
        NotFound = 3
    }
}
