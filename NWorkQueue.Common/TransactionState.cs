using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Common
{
    /// <summary>
    /// The current state of a transaction.
    /// </summary>
    public enum TransactionState
    {
        /// <summary>
        /// This should never occur.  It means there is a serious bug.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Transaction is active.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Transaction has been committed by user.
        /// </summary>
        Commited = 2,

        /// <summary>
        /// Transaction was rolled back by user.
        /// </summary>
        RolledBack = 3,

        /// <summary>
        /// Transaction was automatically expired due to timeout.
        /// </summary>
        Expired = 4,
    }
}
