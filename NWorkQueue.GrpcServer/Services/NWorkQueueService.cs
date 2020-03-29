using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NWorkQueue.Server.Common.Extensions;
using NWorkQueue.Server.Common;
using NWorkQueue.Models;
using NWorkQueue.Common;

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

        public override async Task<GetQueueInfoResponse> GetQueueInfoById(GetQueueInfoByIdRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoById(request.QueueId);
            if (queueInfo == null)
            {
                return new GetQueueInfoResponse { RecordFound = false };
            }

            return new GetQueueInfoResponse { RecordFound = true, QueueId = queueInfo.Id, QueueName = queueInfo.Name };
        }

        public override async Task<GetQueueInfoResponse> GetQueueInfoByName(GetQueueInfoByNameRequest request, ServerCallContext context)
        {
            var queueInfo = await this.internalApi.GetQueueInfoByName(request.QueueName);
            if (queueInfo == null)
            {
                return new GetQueueInfoResponse { RecordFound = false };
            }

            return new GetQueueInfoResponse { RecordFound = true, QueueId = queueInfo.Id, QueueName = queueInfo.Name };
        }

        public override async Task<StartTransactionResponse> StartTransaction(StartTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.StartTrasaction(request.ExpireInMin);
            return new StartTransactionResponse();
        }

        public override async Task<CommitTransactionResponse> CommitTransaction(CommitTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.CommitTransaction(request.TransId);
            return new CommitTransactionResponse();
        }

        /// <inheritdoc/>
        public override async Task<RollbackTransactionResponse> RollbackTranaction(RollbackTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.RollbackTransaction(request.TransId);
            return new RollbackTransactionResponse();
        }

        public override async Task<QueueMessageResponse> QueueMessage(QueueMessageRequest request, ServerCallContext context)
        {
            return await this.internalApi.QueueMessage(
                request.TransId, 
                request.Message.QueueId);
        }

        public override async Task<PullMessageResponse> PullMessages(PullMessageRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

        public override async Task<PeekMessageResponse> PeekMessages(PeekMessageRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }
    }
}
