using System;
using NWorkQueue.Common;

namespace NWorkQueue.TcpClient
{
    public class TcpClient: INWorkQueueClient
    {
        //Create Client with connect string or json
        //Client
        // + queury queues
        // + get statistics
        // -> All methods are also available in the Client, but require string, etc to be passed in instead of Queue or Transaction
        //Queue = Client.GetQueue
        // + Start Transaction
        // + Add Messages
        // + Move Message
        // + Delete Message?
        // + PullMessage + lots of overloads
        //Transaction
        // + Commit
        // + Rollback
    }
}
