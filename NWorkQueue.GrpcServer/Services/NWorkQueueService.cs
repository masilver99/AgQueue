using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NWorkQueue.Server.Common.Extensions;
using NWorkQueue.Server.Common;
using NWorkQueue.Models;

namespace NWorkQueue.GrpcServer
{
    public class NWorkQueueService : QueueApi.QueueApiBase
    {
        private readonly ILogger<NWorkQueueService> _logger;
        private readonly InternalApi internalApi;
        public NWorkQueueService(ILogger<NWorkQueueService> logger, InternalApi internalApi)
        {
            _logger = logger;
            this.internalApi = internalApi;
        }

        public override async Task<CreateQueueResponse> CreateQueue(CreateQueueRequest request, ServerCallContext context)
        {
            RpcException rcpException;
            try
            {
                var result = await this.internalApi.CreateQueue(request.QueueName);
                if (result.ApiResult.IsSuccess)
                {
                    return new CreateQueueResponse { QueueId = result.QueueId };
                }

                rcpException = new RpcException(new Status(result.ApiResult.ResultCode.ToGrpcStatus(), result.ApiResult.Message));
            }
            catch (Exception e)
            {
                rcpException = new RpcException(new Status(StatusCode.Unknown, e.Message));
            }

            throw rcpException;
        }

        public override async Task<InitializeStorageResponse> InitializeStorage(InitializeStorageRequest request, ServerCallContext context)
        {
            await this.internalApi.InitializeStorage(request.DeleteExistingData);
            return new InitializeStorageResponse();
        }

        public override async Task<DeleteQueueByIdResponse> DeleteQueueById(DeleteQueueByIdRequest request, ServerCallContext context)
        {
            // May need to add code to check if messages exist before deleting.
            var result = await this.internalApi.GetQueueName(request.QueueId);
            if (result.ApiResult.IsSuccess)
            {
                return this.ReturnIfSuccess<DeleteQueueByIdResponse>(await this.internalApi.DeleteQueue(request.QueueId));
            }

            throw result.ApiResult.CreateRpcException();
        }

        public override async Task<DeleteQueueByNameResponse> DeleteQueueByName(DeleteQueueByNameRequest request, ServerCallContext context)
        {
            // May need to add code to check if messages exist before deleting.
            var result = await this.internalApi.GetQueueId(request.QueueName);
            if (result.ApiResult.IsSuccess)
            {
                return this.ReturnIfSuccess<DeleteQueueByNameResponse>(await this.internalApi.DeleteQueue(result.QueueId));
            }

            throw result.ApiResult.CreateRpcException();
        }
        /*
        public override async Task<QueueInfoResponse> QueueInfoById(QueueInfoByIdRequest request, ServerCallContext context)
        {
            RpcException rcpException;
            try
            {
                var result = await this.internalApi.CreateQueue(request.QueueName);
                if (result.ApiResult.IsSuccess)
                {
                    return new CreateQueueResponse { QueueId = result.QueueId };
                }

                rcpException = new RpcException(new Status(result.ApiResult.ResultCode.ToGrpcStatus(), result.ApiResult.Message));
            }
            catch (Exception e)
            {
                rcpException = new RpcException(new Status(StatusCode.Unknown, e.Message));
            }

            throw rcpException;

        }

        public override async Task<QueueInfoResponse> QueueInfoByName(QueueInfoByNameRequest request, ServerCallContext context)
        {
        }
        */
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
    }
}
