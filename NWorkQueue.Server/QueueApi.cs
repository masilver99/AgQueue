// <copyright file="QueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Grpc.Core;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;
using NWorkQueue.Library;

namespace NWorkQueue.Server
{
    public class QueueApi : IQueueApi
    {
        private readonly InternalApi internalApi;

        public QueueApi(InternalApi internalApi)
        {
            this.internalApi = internalApi;
        }

        public async ValueTask<CreateQueueResponse> CreateQueue(CreateQueueRequest request)
        {
            try
            {
                var result = await this.internalApi.CreateQueue(request.QueueName);
                if (result.ApiResult.IsSuccess)
                {
                    return new CreateQueueResponse(result.QueueId);
                }

                throw new RpcException(new Status(result.ApiResult.ResultCode.ToGrpcStatus(), result.ApiResult.Message));
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.Unknown, e.Message));
            }
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
