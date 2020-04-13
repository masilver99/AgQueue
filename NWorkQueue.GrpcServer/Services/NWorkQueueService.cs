using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NWorkQueue.Common.Extensions;
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
            var transId = await this.internalApi.StartTrasaction(request.ExpireInMin);
            return new StartTransactionResponse() { TransId = transId };
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
                request.QueueId,
                request.Message.Payload.ToByteArray(),
                request.Message.MetaData,
                request.Message.Priority,
                request.Message.MaxAttempts,
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

            if (message == null)
            {
                return new DequeueMessageResponse
                {
                    TransId = request.TransId,
                    MessageFound = false
                };
            }

            return new DequeueMessageResponse
            {
                Message = new MessageOut()
                {
                    Id = message.Id,
                    CorrelationId = message.CorrelationId,
                    ExpiryDateTime = message.ExpiryDateTime ?? 0, // Zero is a null in protbuf
                    AddDateTime = message.AddDateTime,
                    CloseDateTime = message.CloseDateTime ?? 0,
                    GroupName = message.GroupName,
                    MaxAttempts = message.MaxAttempts,
                    MessageState = (Models.MessageState)message.MessageState,
                    MetaData = message.Metadata,
                    Payload = ByteString.CopyFrom(message.Payload),
                    Priority = message.Priority,
                    QueueId = message.QueueId,
                    TransAction = (Models.TransactionAction)message.TransactionAction,
                    TransId = message.TransactionId
                },
                MessageFound = true
            };
        }

        public override async Task<PeekMessageByQueueResponse> PeekMessageByQueue(PeekMessageByQueueRequest request, ServerCallContext context)
        {
            var message = await this.internalApi.PeekMessageByQueueId(
                request.QueueId);

            return new PeekMessageByQueueResponse
            {
                //  TODO: this needs to be merged into a MessageOut factory
                Message = new MessageOut()
                {
                    Id = message.Id,
                    CorrelationId = message.CorrelationId,
                    ExpiryDateTime = message.ExpiryDateTime ?? 0,
                    AddDateTime = message.AddDateTime,
                    CloseDateTime = message.CloseDateTime ?? 0,
                    GroupName = message.GroupName,
                    MaxAttempts = message.MaxAttempts,
                    Attempts = message.Attempts,
                    MessageState = (Models.MessageState)message.MessageState,
                    MetaData = message.Metadata,
                    Payload = ByteString.CopyFrom(message.Payload),
                    Priority = message.Priority,
                    QueueId = message.QueueId,
                    TransAction = (Models.TransactionAction)message.TransactionAction,
                    TransId = message.TransactionId
                }
            };
        }
        public override async Task<PeekMessageByIdResponse> PeekMessageById(PeekMessageByIdRequest request, ServerCallContext context)
        {
            var message = await this.internalApi.PeekMessageById(
                request.MessageId);

            return new PeekMessageByIdResponse
            {
                Message = new MessageOut()
                {
                    Id = message.Id,
                    CorrelationId = message.CorrelationId,
                    ExpiryDateTime = message.ExpiryDateTime ?? 0,
                    AddDateTime = message.AddDateTime,
                    CloseDateTime = message.CloseDateTime ?? 0,
                    GroupName = message.GroupName,
                    MaxAttempts = message.MaxAttempts,
                    Attempts = message.Attempts,
                    MessageState = (Models.MessageState)message.MessageState,
                    MetaData = message.Metadata,
                    Payload = ByteString.CopyFrom(message.Payload),
                    Priority = message.Priority,
                    QueueId = message.QueueId,
                    TransAction = (Models.TransactionAction)message.TransactionAction,
                    TransId = message.TransactionId
                }
            };
        }
    }
}
