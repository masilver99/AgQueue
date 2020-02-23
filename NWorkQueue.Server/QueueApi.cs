using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NWorkQueue.Server
{
    public class QueueApi : IQueueApi
    {
        ValueTask Test()
        {
            throw new RpcException
        }
        //CreateQueue
        //DeleteQueue
        //AddMessage
        //PeekMessage
        //PullMessage
    }
}
