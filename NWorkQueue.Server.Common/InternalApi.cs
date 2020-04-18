// <copyright file="InternalApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NWorkQueue.Common;
using NWorkQueue.Common.Extensions;
using NWorkQueue.Common.Models;
using NWorkQueue.Server.Common.Models;

[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Server.Common
{
    /// <summary>
    /// Starting point for accessing all queue related APIS
    /// This is mostly a factory for creating Queues and Transactions.
    /// </summary>
    /// <remarks>
    /// Exceptions are not used unless there is an exceptional condition.  For example, if an items doesn't exist or a param is invalid,
    /// this is handled without an exception.  This is mostly for speed and simplicity with the gRPC interface.
    /// </remarks>
    public class InternalApi : IDisposable
    {
        private readonly IStorage storage;

        private readonly QueueOptions queueOptions;

        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalApi"/> class.
        /// </summary>
        /// <param name="storage">The storage implementation to use for storage of queues and messages.</param>
        /// <param name="options">The QueueOptions object passed in via DI.</param>
        public InternalApi(IStorage storage, QueueOptions options, ILogger<InternalApi> logger)
        {
            this.logger = logger;

            // Setup Storage
            this.storage = storage;

            this.queueOptions = options;

            // A transaction must have an expiration date, this ensures it.
            if (this.queueOptions.DefaultTranactionTimeoutInMinutes <= 0)
            {
                this.queueOptions.DefaultTranactionTimeoutInMinutes = 10;
                logger.LogWarning("Invalid value for DefaultTranactionTimeoutInMinutes. Using a value of 10.");
            }

            // Check Message timeout is valid.
            if (this.queueOptions.DefaultMessageTimeoutInMinutes <= 0)
            {
                this.queueOptions.DefaultMessageTimeoutInMinutes = 10;
                logger.LogWarning("Invalid value for DefaultMessageTimeoutInMinutes. Using a value of 0 (no timeout).");
            }
        }

        /// <summary>
        /// Creates a new queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <returns>A Queue info object.  Note: the queueName is the stadnardized name.</returns>
        public async ValueTask<QueueInfo> CreateQueue(string queueName)
        {
            var fixedName = queueName.StandardizeQueueName();

            var queueInfo = await this.GetQueueInfoByName(fixedName);

            // Check if Queue already exists
            if (queueInfo != null)
            {
                throw new Exception($"Queue name already exists. {queueName}");
            }

            return new QueueInfo { Id = await this.storage.AddQueue(fixedName), Name = queueName };
        }

        /// <summary>
        /// Delete a queue and all messages in the queue.
        /// Throws an exception if Queue doesn't exist.
        /// </summary>
        /// <param name="queueName">Name of the queue to delete.</param>
        public async void DeleteQueue(string queueName)
        {
            var fixedName = queueName.StandardizeQueueName();
            if (fixedName.Length == 0)
            {
                throw new ArgumentNullException("Queue name is empty");
            }

            var queueInfo = await this.GetQueueInfoByName(fixedName);

            // QueueInfo should never be null if IsSuccess = true
            if (queueInfo == null)
            {
                throw new Exception($"Queue name not found: {queueName}.");
            }
        }

        /// <summary>
        /// Returns information about the requested queue.
        /// </summary>
        /// <param name="queueId">The ID of the queue.</param>
        /// <returns>QueueInfo object. Returns null if not found.</returns>
        public async ValueTask<QueueInfo?> GetQueueInfoById(long queueId)
        {
            return await this.storage.GetQueueInfoById(queueId);
        }

        /// <summary>
        /// Returns information about the requested queue.
        /// </summary>
        /// <param name="queueName">The Name of the queue to lookup.</param>
        /// <returns>QueueInfo object.  Null if not found.</returns>
        public async ValueTask<QueueInfo?> GetQueueInfoByName(string queueName)
        {
            return await this.storage.GetQueueInfoByName(queueName.StandardizeQueueName());
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue.
        /// </summary>
        /// <param name="queueId">Queue id.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask DeleteQueue(long queueId)
        {
            var queueInfo = await this.GetQueueInfoById(queueId);

            if (queueInfo == null)
            {
                throw new Exception($"Queue ID not found: {queueId}.");
            }

            await this.storage.DeleteQueue(queueId);
        }

        /// <summary>
        /// Disposes of storage resources.
        /// </summary>
        public void Dispose()
        {
            this.storage.Dispose();
            // TODO: Need to add OnDsiapose action in case some storage services require disposal
        }

        /// <summary>
        /// Creates underlying structure in the storage layer.
        /// </summary>
        /// <param name="deleteExistingData">Will delete and recreate underlying structures.</param>
        /// <returns>Returns ValueTask.</returns>
        public async ValueTask InitializeStorage(bool deleteExistingData)
        {
            await this.storage.InitializeStorage(deleteExistingData);
        }

        /// <summary>
        /// Starts a transaction used my add message and pull message.
        /// </summary>
        /// <param name="expiryTimeInMinutes">Override default expiration time.</param>
        /// <returns>Transaction ID.</returns>
        public async ValueTask<long> StartTrasaction(int expiryTimeInMinutes = 0)
        {
            // TODO: This 10 needs to be pulled from a config file
            var expiryMinutes = expiryTimeInMinutes <= 0 ?
                this.queueOptions.DefaultTranactionTimeoutInMinutes :
                expiryTimeInMinutes;
            var curDateTime = DateTime.Now;

            return await this.storage.StartTransaction(curDateTime, curDateTime.AddMinutes(expiryMinutes));
        }

        /// <summary>
        /// Extends the transaction by x number of minutes (from the current datetime).
        /// </summary>
        /// <param name="transId">The transaction to extend.</param>
        /// <param name="expiryTimeInMinutes">How long to extend the transaction by.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask ExtendTransaction(long transId, int expiryTimeInMinutes = 0)
        {
            var expiryMinutes = expiryTimeInMinutes <= 0 ?
                this.queueOptions.DefaultTranactionTimeoutInMinutes :
                expiryTimeInMinutes;

            // Perform house cleaning (expire expired trans and messages)
            var startDateTime = DateTime.Now;
            await this.PerformTransactionHouseCleaning(startDateTime);
            await this.PerformMessageHouseCleaning(startDateTime);

            // Check transaction is exists and is active
            await this.ConfirmTransactionExistsAndIsActive(transId);

            // Update Transaction
            await this.storage.ExtendTransaction(transId, DateTime.Now.AddMinutes(expiryMinutes));
        }

        /// <summary>
        /// View the next message in the spcified queue.
        /// </summary>
        /// <remarks>Keep in mind, this may change by the time of the dequeue call.</remarks>
        /// <param name="queueId">The queue to peek from.</param>
        /// <returns>Message object or null if no message available.</returns>
        public async ValueTask<Message?> PeekMessageByQueueId(long queueId)
        {
            // Perform house cleaning (expire expired trans and messages)
            var startDateTime = DateTime.Now;
            await this.PerformTransactionHouseCleaning(startDateTime);
            await this.PerformMessageHouseCleaning(startDateTime);

            return await this.storage.PeekMessageByQueueId(
                queueId);
        }

        /// <summary>
        /// Retrieve a message by it's message ID.
        /// </summary>
        /// <remarks>Keep in mind, this may change by the time of the dequeue call.</remarks>
        /// <param name="messageId">The ID of the message to retrieve.</param>
        /// <returns>Message object or null if no message available.</returns>
        public async ValueTask<Message?> PeekMessageById(long messageId)
        {
            // Perform house cleaning (expire expired trans and messages)
            var startDateTime = DateTime.Now;
            await this.PerformTransactionHouseCleaning(startDateTime);
            await this.PerformMessageHouseCleaning(startDateTime);

            return await this.storage.PeekMessageByMessageId(
                messageId);
        }

        /// <summary>
        /// Commits Transaction, updating all messages in transaction.
        /// </summary>
        /// <param name="transId">Transaction Id to commit.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask<(int AddCount, int PullCount)> CommitTransaction(long transId)
        {
            var startDateTime = DateTime.Now;

            // Perform house cleaning (expire expired trans and messages)
            await this.PerformTransactionHouseCleaning(startDateTime);

            await this.PerformMessageHouseCleaning(startDateTime);

            // Check transaction is exists and is active
            await this.ConfirmTransactionExistsAndIsActive(transId);

            var storageTrans = this.storage.BeginStorageTransaction();

            try
            {
                // Change status of added messages
                var addCount = await this.storage.UpdateMessages(storageTrans, transId, TransactionAction.Add, MessageState.InTransaction, MessageState.Active, null);

                // Change status of pulled messages
                var pullCount = await this.storage.UpdateMessages(storageTrans, transId, TransactionAction.Pull, MessageState.InTransaction, MessageState.Processed, startDateTime);

                // Mark Transaction complete
                await this.storage.UpdateTransactionState(storageTrans, transId, TransactionState.Commited, "Committed", startDateTime);

                // Commit DB Trans ---
                storageTrans.Commit();

                return (addCount, pullCount);
            }
            catch
            {
                storageTrans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Rollsback a transaction, undoing any changes to messages in the transaction.
        /// </summary>
        /// <param name="transId">The transaction ID of the transaction to rollback.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask RollbackTransaction(long transId)
        {
            var startDateTime = DateTime.Now;

            // Perform house cleaning (expire expired trans and messages)
            await this.PerformTransactionHouseCleaning(startDateTime);

            // Check transaction is exists and is active
            await this.ConfirmTransactionExistsAndIsActive(transId);

            var storageTrans = this.storage.BeginStorageTransaction();

            try
            {
                // Change status of added messages
                var deletedCount = await this.storage.DeleteAddedMessages(storageTrans, transId);

                // Update retry counts and change status of pulled messages
                // Don't worry about retry count here, it will automactially get closed in house keeping.
                var updateCount = await this.storage.UpdateMessageAttemptCount(storageTrans, transId, TransactionAction.Pull, MessageState.InTransaction);
                var pullCount = await this.storage.UpdateMessages(storageTrans, transId, TransactionAction.Pull, MessageState.InTransaction, MessageState.Active, null);

                // Mark Transaction complete
                await this.storage.UpdateTransactionState(storageTrans, transId, TransactionState.RolledBack, "User rollback", DateTime.Now);

                // Commit DB Trans ---
                storageTrans.Commit();
            }
            catch
            {
                storageTrans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Adds a message to the specified queue.
        /// </summary>
        /// <param name="transId">The transaction to add the message in.</param>
        /// <param name="queueId">The queue the message will be placed in.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="metaData">Message metadata.</param>
        /// <param name="priority">The message priority.  Higher numbers will have higher priority.</param>
        /// <param name="maxRetries">How many times should the message pull fail before being retired.</param>
        /// <param name="expiryInMinutes">How long in minutes before the message will expire before it's pulled.  0 = no expiration.</param>
        /// <param name="correlation">Correlation number.</param>
        /// <param name="groupName">Group name applied to message.</param>
        /// <returns>Return the message id.</returns>
        public async ValueTask<long> QueueMessage(
            long transId,
            long queueId,
            byte[] payload,
            string metaData,
            int priority,
            int maxRetries,
            int expiryInMinutes,
            int correlation,
            string groupName)
        {
            var startDateTime = DateTime.Now;

            // Perform house cleaning (expire expired trans and messages)
            await this.PerformTransactionHouseCleaning(startDateTime);

            // Check transaction exists and is active
            await this.ConfirmTransactionExistsAndIsActive(transId);

            // Does queue exist
            var queueInfo = this.storage.GetQueueInfoById(queueId);
            if (queueInfo == null)
            {
                throw new WorkQueueException($"Queue {queueId} not found.");
            }

            var calculatedExpiry = expiryInMinutes <= 0 ?
                this.queueOptions.DefaultMessageTimeoutInMinutes :
                expiryInMinutes;

            return await this.storage.AddMessage(
                transId,
                queueId,
                payload,
                startDateTime,
                metaData,
                priority,
                maxRetries,
                // I was going to allow umlimited time, but I'm rethrinking that.
                startDateTime.AddMinutes(calculatedExpiry),
                correlation,
                groupName,
                TransactionAction.Add,
                MessageState.InTransaction);
        }

        /// <summary>
        /// Dequeues a message from a specific queue.
        /// </summary>
        /// <param name="transId">The transaction ID of the transaction the message will be dequeued in.</param>
        /// <param name="queueId">The queue to pull the messge from.</param>
        /// <returns>Returns a message object or null if there is no message to dequeue.</returns>
        public async ValueTask<Message?> DequeueMessage(
            long transId,
            long queueId)
        {
            // Perform house cleaning (expire expired trans and messages)
            var startDateTime = DateTime.Now;
            await this.PerformTransactionHouseCleaning(startDateTime);
            await this.PerformMessageHouseCleaning(startDateTime);

            // Check count is above min
            // Pull Records, update them to be in transaction
            return await this.storage.DequeueMessage(
                transId,
                queueId);
        }

        private async ValueTask ConfirmTransactionExistsAndIsActive(long transId)
        {
            var trans = await this.storage.GetTransactionById(transId);
            if (trans == null)
            {
                throw new Exception($"Transaction not found, id: {transId}");
            }

            if (trans.State != TransactionState.Active)
            {
                throw new Exception($"Transaction {transId} not active: {trans.State}");
            }
        }

        /// <summary>
        /// Expires transactions, message and checks message counts, maybe.
        /// </summary>
        /// <param name="currentTime">The current time is passed in so it is consistent with the time used in the calling procedure.</param>
        /// <returns>ValueTask.</returns>
        private async ValueTask PerformTransactionHouseCleaning(DateTime currentTime)
        {
            // Check is any active transactions have expired
            // Start db transaction
            var storageTrans = this.storage.BeginStorageTransaction();

            // Delete any added messages (may want to mark as orphaned, instead of deleting)
            await this.storage.DeleteAddedMessagesInExpiredTrans(storageTrans, currentTime);

            // Increment retry count on pulled messages.
            // Removed trans info from pulled messages
            await this.storage.UpdateMessageAttemptsInExpiredTrans(storageTrans, currentTime);

            // Mark trans as closed due to expiry
            await this.storage.ExpireTransactions(storageTrans, currentTime);

            // Commit db transaction
            storageTrans.Commit();
        }

        private async ValueTask PerformMessageHouseCleaning(DateTime currentDateTime)
        {
            // Check for any active but expired Messages (NOT IN A TRANSACTION) (Messages in an active transaction won't expired (they are safe until the transaction commits or rollbacks))
            // Mark them as expired
            await this.storage.ExpireMessages(currentDateTime);

            // Check for active messages at or past the retry count
            await this.storage.CloseMaxAttemptsExceededMessages(currentDateTime);

                // Mark as RetryExceeded
        }
    }
}
