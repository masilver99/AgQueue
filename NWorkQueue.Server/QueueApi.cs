// <copyright file="QueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;

namespace NWorkQueue.Server
{
    public class QueueApi : IQueueApi
    {
        public ValueTask<CreateQueueResponse> CreateQueue(CreateQueueRequest request)
        {
            return new ValueTask<CreateQueueResponse>(new CreateQueueResponse());
        }
        //CreateQueue
        //DeleteQueue
        //AddMessage
        //PeekMessage
        //PullMessage
    }
}
