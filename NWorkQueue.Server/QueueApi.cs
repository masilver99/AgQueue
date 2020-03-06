// <copyright file="QueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Grpc.Core;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;
using NWorkQueue.Library;
using NWorkQueue.Library.Extensions;

namespace NWorkQueue.Server
{
    /// <summary>
    /// Contains service methods for accessing and manipulating queues.
    /// </summary>
    /// <remarks>
    /// All methods expect one object to bew returned and one object as a parameter.  This is inline with gRPC design considerations.
    /// This allows for expansion of parameters without causing breaking changes in the client library.
    /// </remarks>
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
            RpcException rcpException;
            try
            {
                var result = await this.internalApi.CreateQueue(request.QueueName);
                if (result.ApiResult.IsSuccess)
                {
                    return new CreateQueueResponse(result.QueueId);
                }

                rcpException = new RpcException(new Status(result.ApiResult.ResultCode.ToGrpcStatus(), result.ApiResult.Message));
            }
            catch (Exception e)
            {
                rcpException = new RpcException(new Status(StatusCode.Unknown, e.Message));
            }

            throw rcpException;
        }

        /// <summary>
        /// Creates the underlying storage for the queue.
        /// </summary>
        /// <param name="request">InitializeStorageRequest object.  Note: if DeleteExistingData is true, all data will be deleted.</param>
        /// <returns>ValueTask for async.</returns>
        public async ValueTask<InitializeStorageResponse> InitializeStorage(InitializeStorageRequest request)
        {
            await this.internalApi.InitializeStorage(request.DeleteExistingData);
            return new InitializeStorageResponse();
        }

        public async ValueTask<DeleteQueueResponse> DeleteQueue(DeleteQueueByNameRequest request)
        {
            // May need to add code to check if messages exist before deleting.
            var result = await this.internalApi.GetQueueId(request.QueueName);
            if (result.ApiResult.IsSuccess)
            {
                return this.ReturnIfSuccess<DeleteQueueResponse>(await this.internalApi.DeleteQueue(result.QueueId));
            }

            throw result.ApiResult.CreateRpcException();
        }

        public async ValueTask<DeleteQueueResponse> DeleteQueue(DeleteQueueByIdRequest request)
        {
            // May need to add code to check if messages exist before deleting.
            var result = await this.internalApi.GetQueueName(request.QueueId);
            if (result.ApiResult.IsSuccess)
            {
                return this.ReturnIfSuccess<DeleteQueueResponse>(await this.internalApi.DeleteQueue(request.QueueId));
            }

            throw result.ApiResult.CreateRpcException();
        }

        private bool IsApiSuccess(ApiResult apiResult)
        {
            if (apiResult.IsSuccess)
            {
                return true;
            }
            else
            {
                throw apiResult.CreateRpcException();
            }
        }

        private T ReturnIfSuccess<T>(ApiResult apiResult)
            where T : new()
        {
            if (apiResult.IsSuccess)
            {
                return new T();
            }
            else
            {
                throw apiResult.CreateRpcException();
            }
        }

        //AddMessage
        ///PeekMessage
        //PullMessage
    }
}
