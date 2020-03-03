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
    /// <summary>
    /// Contains service methods for accessing and manipulating queues.
    /// </summary>
    public class QueueApi : IQueueApi
    {
        private readonly InternalApi internalApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueApi"/> class.
        /// </summary>
        /// <param name="internalApi">Internal API library used to access the queues.</param>
        public QueueApi(InternalApi internalApi)
        {
            this.internalApi = internalApi;
        }

        /// <summary>
        /// Creates a queue.  If name already exists, an exception will be thrown.
        /// </summary>
        /// <param name="request"><see cref="CreateQueueRequest"/> object.</param>
        /// <returns><see cref="CreateQueueResponse"/> object.</returns>
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

        /// <summary>
        /// Creates the underlying storage for the queue.  
        /// </summary>
        /// <param name="request">InitializeStorageRequest object.  Note: if DeleteExistingData is true, all data will be deleted.</param>
        /// <returns>ValueTask for async.</returns>
        public async ValueTask InitializeStorage(InitializeStorageRequest request)
        {
            await this.internalApi.InitializeStorage(request.DeleteExistingData);
        }

        public async ValueTask DeleteQueue(DeleteQueueByNameRequest request)
        {

        }

        public async ValueTask DeleteQueue(DeleteQueueByIdRequest request)
        {

        }


        //DeleteQueue
        //AddMessage
        ///PeekMessage
        //PullMessage
    }
}
