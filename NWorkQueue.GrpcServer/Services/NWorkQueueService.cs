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
            var queueInfo = await this.internalApi.CreateQueue(request.QueueName);
            return new CreateQueueResponse { QueueId = queueInfo.Id, QueueName = queueInfo.Name };
        }

        public override async Task<InitializeStorageResponse> InitializeStorage(InitializeStorageRequest request, ServerCallContext context)
        {
            await this.internalApi.InitializeStorage(request.DeleteExistingData);
            return new InitializeStorageResponse();
        }

        public override async Task<DeleteQueueByIdResponse> DeleteQueueById(DeleteQueueByIdRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoById(request.QueueId);
            if (queueInfo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Queue not found: {request.QueueId}"));
            }
            await this.internalApi.DeleteQueue(request.QueueId);
            return  new DeleteQueueByIdResponse();
        }

        public override async Task<DeleteQueueByNameResponse> DeleteQueueByName(DeleteQueueByNameRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoByName(request.QueueName);
            if (queueInfo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Queue not found: {request.QueueName}"));
            }
            await this.internalApi.DeleteQueue(queueInfo.Id);
            return new DeleteQueueByNameResponse();
        }

        public override async Task<QueueInfoResponse> QueueInfoById(QueueInfoByIdRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoById(request.QueueId);
            if (queueInfo == null)
            {
                return new QueueInfoResponse { RecordFound = false };
            }

            return new QueueInfoResponse { RecordFound = true, QueueId = queueInfo.Id, QueueName = queueInfo.Name };
        }

        public override async Task<QueueInfoResponse> QueueInfoByName(QueueInfoByNameRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoByName(request.QueueName);
            if (queueInfo == null)
            {
                return new QueueInfoResponse { RecordFound = false };
            }

            return new QueueInfoResponse { RecordFound = true, QueueId = queueInfo.Id, QueueName = queueInfo.Name };
        }

        public override async Task<StartTransactionResponse> StartTransaction(StartTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.StartTrasaction(request.ExpireInMin);
            return new StartTransactionResponse();
        }

        public override async Task<CommitTransactionResponse> CommitTransaction(CommitTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.CommitTrasaction(request.TransId);
            return new CommitTransactionResponse();
        }

        public override async Task<RollbackTransactionResponse> RollbackTranaction(RollbackTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.RollbackTrasaction(request.TransId);
            return new RollbackTransactionResponse();
        }
    }
}
