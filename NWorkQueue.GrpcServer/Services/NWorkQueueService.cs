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
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;

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
            var (addCount, pullCount) = await this.internalApi.CommitTransaction(request.TransId);
            return new CommitTransactionResponse { MessagesAdded = addCount, MessagesPulled = pullCount };
        }

        /// <inheritdoc/>
        public override async Task<RollbackTransactionResponse> RollbackTranaction(RollbackTransactionRequest request, ServerCallContext context)
        {
            await this.internalApi.RollbackTransaction(request.TransId);
            return new RollbackTransactionResponse();
        }

        public override async Task<QueueMessageResponse> QueueMessage(QueueMessageRequest request, ServerCallContext context)
        {
            var messageId = await this.internalApi.QueueMessage(
                request.TransId, 
                request.Message.QueueId,
                request.Message.Payload.ToByteArray(),
                request.Message.MetaData,
                request.Message.Priority,
                request.Message.MaxRetries,
                request.Message.ExpiryInMinutes,
                request.Message.CorrelationId,
                request.Message.GroupName);
            return new QueueMessageResponse { MessageId = messageId, TransId = request.TransId };
        }

        public override async Task<DequeueMessageResponse> DequeueMessage(DequeueMessageRequest request, ServerCallContext context)
        {
            var message = await this.internalApi.DequeueMessage(
                request.TransId,
                request.QueueId);

            return new DequeueMessageResponse
            {
                Message = new MessageOut()
                {
                    CorrelationId = message.CorrelationId,
                    ExpiryDateTime = Timestamp.FromDateTime(message.ExpiryDateTime),
                    AddDateTime = Timestamp.FromDateTime(message.AddDateTime),
                    CloseDateTime = Timestamp.FromDateTime(message.CloseDateTime),
                    GroupName = message.GroupName,
                    MaxRetries = message.MaxRetries,
                    MessageState = (Models.MessageState)message.MessageState.Value,
                    MetaData = message.Metadata,
                    Payload = ByteString.CopyFrom(message.Payload),
                    Priority = message.Priority,
                    QueueId = message.QueueId,
                    TransAction = (Models.TransactionAction)message.TransactionAction.Value,
                    TransId = message.TransactionId
                }
            };
        }

        public override async Task<PeekMessageByQueueResponse> PeekMessageByQueue(PeekMessageByQueueRequest request, ServerCallContext context)
        {
            var message = await this.internalApi.PeekMessageByQueueId(
                request.QueueId);

            return new PeekMessageByQueueResponse
            {
                Message = new MessageOut()
                {
                    CorrelationId = message.CorrelationId,
                    ExpiryDateTime = Timestamp.FromDateTime(message.ExpiryDateTime),
                    AddDateTime = Timestamp.FromDateTime(message.AddDateTime),
                    CloseDateTime = Timestamp.FromDateTime(message.CloseDateTime),
                    GroupName = message.GroupName,
                    MaxRetries = message.MaxRetries,
                    MessageState = (Models.MessageState)message.MessageState.Value,
                    MetaData = message.Metadata,
                    Payload = ByteString.CopyFrom(message.Payload),
                    Priority = message.Priority,
                    QueueId = message.QueueId,
                    TransAction = (Models.TransactionAction)message.TransactionAction.Value,
                    TransId = message.TransactionId
                }
            };
        }
    }
}
