// <copyright file="QueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;
using NWorkQueue.Library;

namespace NWorkQueue.Server
{
    public class QueueApi : IQueueApi
    {
        private InternalApi internalApi;

        public QueueApi(InternalApi internalApi)
        {
            this.internalApi = internalApi;
        }

        public ValueTask<CreateQueueResponse> CreateQueue(CreateQueueRequest request)
        {
            return new ValueTask<CreateQueueResponse>(new CreateQueueResponse());
        }

        public ValueTask<ActionResponse> InitializeStorage(InitializeStorageRequest request)
        {
            this.internalApi.InitializeStorage(request.DeleteExistingData);
            return new ValueTask<ActionResponse>(new ActionResponse { Success = true });
        }

        //DeleteQueue
        //AddMessage
        //PeekMessage
        //PullMessage
    }
}
