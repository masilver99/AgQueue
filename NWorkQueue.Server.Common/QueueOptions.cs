using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Server.Common
{
    /// <summary>
    /// Options used by the queue server.
    /// </summary>
    public class QueueOptions
    {
        /// <summary>
        /// How many minutes until a message expires.  Will be overriden by the value in the message, unless it's zero.
        /// 0 = max message timeout.  0 in the message, means use the detault here..
        /// </summary>
        public int DefaultMessageTimeoutInMinutes { get; set; } = 10;

        /// <summary>
        /// How long until a transaction expires in minutes.  The value in the transaction will override this value, 
        /// unless it zero.  If this is zero as well, transactions won't expire.
        /// </summary>
        public int DefaultTranactionTimeoutInMinutes { get; set; } = 10;
    }
}
