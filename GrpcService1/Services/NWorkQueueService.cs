using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NWorkQueue.Library.Extensions;
using NWorkQueue.Library;
using NWorkQueue.Models;

namespace NWorkQueue.GrpcService
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
                    return new CreateQueueResponse();// result.QueueId);
                }

                rcpException = new RpcException(new Status(result.ApiResult.ResultCode.ToGrpcStatus(), result.ApiResult.Message));
            }
            catch (Exception e)
            {
                rcpException = new RpcException(new Status(StatusCode.Unknown, e.Message));
            }

            throw rcpException;
        }

        public virtual Task<InitializeStorageResponse> InitializeStorage(InitializeStorageRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public virtual Task<DeleteQueueByIdResponse> DeleteQueueById(DeleteQueueByIdRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public virtual Task<DeleteQueueByIdResponse> DeleteQueueByName(DeleteQueueByNameRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

    }
}
