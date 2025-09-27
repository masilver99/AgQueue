// <copyright file="IStorage.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Procession.Common;
using Procession.Common.Models;
using Procession.Server.Common.Models;

namespace Procession.Server.Common
{
    /// <summary>
    /// The interface for storing and retrieving queue information from a storage mechinism, usually a database.
    /// When implementing IStorage, use the StorageSqlite as an example.  There should be no business logic in
    /// classes that implement IStorage.
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Called when Queue process starts.  Connections to the storage should be made here, etc.
        /// </summary>
        /// <param name="deleteExistingData">Should all existing queues and messages be deleted.</param>
        /// <returns>ValueTask.</returns>
        ValueTask InitializeStorage(bool deleteExistingData);

        /// <summary>
        /// Create a new Queue in storage.
        /// </summary>
        /// <param name="name">Queue name.</param>
        /// <returns>ValueTask.</returns>
        ValueTask<long> AddQueue(string name);

        /// <summary>
        /// Delete a Queue and ALL messages in the Queue.
        /// </summary>
        /// <param name="id">Queue Id of the queue to delete.</param>
        /// <returns>ValueTask.</returns>
        ValueTask DeleteQueue(long id);

        /// <summary>
        /// Returns a Transaction object or null value if not found.
        /// </summary>
        /// <param name="transId">The Id of the transaction to lookup.</param>
        /// <returns>Transaction object or null if not found.</returns>
        ValueTask<Transaction?> GetTransactionById(long transId);

        /// <summary>
        /// Add a message to the storage.
        /// </summary>
        /// <param name="transId">Queue Transaction ID.</param>
        /// <param name="queueId">ID of the storage queue.</param>
        /// <param name="payload">Message data.</param>
        /// <param name="addDateTime">Datetime the message was added.</param>
        /// <param name="metaData">String metadata on message data.</param>
        /// <param name="priority">Message priority.</param>
        /// <param name="maxAttempts">How many retries before expires.</param>
        /// <param name="expiryDateTime">Datetime the message will expire.</param>
        /// <param name="correlation">Correlation ID.</param>
        /// <param name="groupName">Group name.</param>
        /// <param name="transactionAction">Always will be "add".</param>
        /// <param name="messageState">"Always will be in transaction.</param>
        /// <returns>Message ID.</returns>
        ValueTask<long> AddMessage(
            long transId,
            long queueId,
            byte[] payload,
            DateTime addDateTime,
            string metaData,
            int priority,
            int maxAttempts,
            DateTime? expiryDateTime,
            int correlation,
            string groupName,
            TransactionAction transactionAction,
            MessageState messageState);

        /// <summary>
        /// Returns the id and name of the Queue.  If no queue is found, returns null.
        /// </summary>
        /// <remarks>
        /// This search should be case sensitive, only use LIKE with SQLite.
        /// </remarks>
        /// <param name="name">Name of the queue to lookup.</param>
        /// <returns>QueueInfo containing ID and Name of queue.  Null if not found.</returns>
        ValueTask<QueueInfo?> GetQueueInfoByName(string name);

        /// <summary>
        /// Returns the id and name of the Queue.  If no queue is found, returns null.
        /// </summary>
        /// <remarks>
        /// This search should be case sensitive, only use LIKE with SQLite.
        /// </remarks>
        /// <param name="queueId">ID of the queue to lookup.</param>
        /// <returns>QueueInfo containing ID and Name of queue.  Null if not found.</returns>
        ValueTask<QueueInfo?> GetQueueInfoById(long queueId);

        /// <summary>
        /// Starts a transaction for use when adding or pulling messages.
        /// </summary>
        /// <param name="startDateTime">When the transaction started.</param>
        /// <param name="expiryDateTime">When the transaction will end.</param>
        /// <returns>Returns transaction ID.</returns>
        ValueTask<long> StartTransaction(DateTime startDateTime, DateTime expiryDateTime);

        /// <summary>
        /// Updates a set of messages.
        /// </summary>
        /// <param name="storageTrans">The storage transaction to make change within.</param>
        /// <param name="transId">The transaction the messages are associated with.</param>
        /// <param name="transactionAction">The TransactionAction to search for.</param>
        /// <param name="oldMessageState">Current message state.</param>
        /// <param name="newMessageState">Update the message state to this value.</param>
        /// <returns>Returns the number of messages updated.</returns>
        ValueTask<int> UpdateMessages(IStorageTransaction storageTrans, long transId, TransactionAction transactionAction, MessageState oldMessageState, MessageState newMessageState, DateTime? closeDateTime);

        /// <summary>
        /// Updates the message retry cuont based on transactionAction and MessageState.
        /// </summary>
        /// <param name="storageTrans">The storage transaction to run the query under.</param>
        /// <param name="transId">The messages must be in this transaction.</param>
        /// <param name="transactionAction">The transactionAction the message must be in.</param>
        /// <param name="messageState">The messageState the message must be in. </param>
        /// <returns>Number of messages updated.</returns>
        ValueTask<int> UpdateMessageAttemptCount(IStorageTransaction storageTrans, long transId, TransactionAction transactionAction, MessageState messageState);

        /// <summary>
        /// Delete messages that were added in the specified transtacion.
        /// </summary>
        /// <param name="storageTrans">The storage transaction to run the query in.</param>
        /// <param name="transId">The transaction the messages must be in.</param>
        /// <returns>Number of records deleted.</returns>
        ValueTask<int> DeleteAddedMessages(IStorageTransaction storageTrans, long transId);

        /// <summary>
        /// Extends the transaction's expiration datetime.
        /// </summary>
        /// <param name="transId">The id of the transaction to update.</param>
        /// <param name="expiryDateTime">The new expiration datetime.</param>
        /// <returns>ValueTask.</returns>
        ValueTask ExtendTransaction(long transId, DateTime expiryDateTime);

        /// <summary>
        /// Update the transaction's state and end datetime.
        /// </summary>
        /// <param name="storageTrans">The storage transaction to make change within.</param>
        /// <param name="transId">The id of the transaction to update.</param>
        /// <param name="state">The new state of the transaction.</param>
        /// <param name="endReason">Reason the transaction was ended.  Optional.</param>
        /// <param name="endDateTime">Datetime the transaction was closed (or null if not closed).</param>
        /// <returns>ValueTask.</returns>
        ValueTask UpdateTransactionState(IStorageTransaction storageTrans, long transId, TransactionState state, string? endReason = null, DateTime? endDateTime = null);

        /// <summary>
        /// Starts a storage (database) transaction, not a queue transaction.
        /// </summary>
        /// <remarks>
        /// Not all Storage classes will have internal transactions, so this can return a dummy class that performs no actions.
        /// </remarks>
        /// <returns>Returns a class represented by IStorageTransaction which can commit or rollbacl the transaction.</returns>
        IStorageTransaction BeginStorageTransaction();

        /// <summary>
        /// Deletes added messages in an expired transaction.
        /// </summary>
        /// <param name="storageTrans">Storage Transaction to run under.</param>
        /// <param name="currentDateTime">DateTime to expire against.</param>
        /// <returns>Returns the count of the deleted records.</returns>
        ValueTask<int> DeleteAddedMessagesInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime);

        /// <summary>
        /// Updates the retry counts for messages in an expired transaction.
        /// </summary>
        /// <param name="storageTrans">Storage Transaction to run under.</param>
        /// <param name="currentDateTime">DateTime to expire against.</param>
        /// <returns>Number of records updated.</returns>
        ValueTask<int> UpdateMessageAttemptsInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime);

        /// <summary>
        /// Expires transactions whose expiry date time is past the currentDateTime.
        /// </summary>
        /// <param name="storagetrans">Storage Transaction to run under.</param>
        /// <param name="currentDateTime">DateTime to expire against.</param>
        /// <returns>Returns the number of transactions expired.</returns>
        ValueTask<int> ExpireTransactions(IStorageTransaction storagetrans, DateTime currentDateTime);

        /// <summary>
        /// Expire messages that are past their expiration date.
        /// </summary>
        /// <param name="currentDateTime">Date used to validate expiration date against.  Also to be used as the close datetime.</param>
        /// <returns>Number of messages updated.</returns>
        ValueTask<int> ExpireMessages(DateTime currentDateTime);

        /// <summary>
        /// Close message that are beyond their retry count.
        /// </summary>
        /// <param name="currentDateTime">DateTime to use a close datetime.</param>
        /// <returns>Number of messages closed.</returns>
        ValueTask<int> CloseMaxAttemptsExceededMessages(DateTime currentDateTime);

        /// <summary>
        /// Dequeue the next message in the queue and flag the message with the transaction id.
        /// </summary>
        /// <param name="transId">The transaction id to add to the message.</param>
        /// <param name="queueId">The queue to pull the message frrom.</param>
        /// <returns>Message object or null of no message to pull.</returns>
        ValueTask<Message?> DequeueMessage(long transId, long queueId);

        /// <summary>
        /// View the next message in the queue, without removing it from queue.
        /// </summary>
        /// <param name="queueId">The queue to pull the message from.</param>
        /// <returns>Message object or null of no message to pull.</returns>
        ValueTask<Message?> PeekMessageByQueueId(long queueId);

        /// <summary>
        /// View a message by the message ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to view.</param>
        /// <returns>Message object or null of no message to view.</returns>
        ValueTask<Message?> PeekMessageByMessageId(long messageId);

        /// <summary>
        /// Get all queues in the storage.
        /// </summary>
        /// <returns>List of all QueueInfo objects.</returns>
        ValueTask<List<QueueInfo>> GetAllQueues();

        /// <summary>
        /// Get messages with paging and filtering support.
        /// </summary>
        /// <param name="offset">Number of records to skip.</param>
        /// <param name="limit">Maximum number of records to return.</param>
        /// <param name="queueId">Optional queue ID to filter by. If null, gets messages from all queues.</param>
        /// <param name="processedOnly">Optional filter: true for processed only, false for unprocessed only, null for all.</param>
        /// <returns>List of messages.</returns>
        ValueTask<List<Message>> GetMessages(int offset, int limit, long? queueId = null, bool? processedOnly = null);

        /// <summary>
        /// Get message count statistics by state.
        /// </summary>
        /// <returns>Dictionary with MessageState as key and count as value.</returns>
        ValueTask<Dictionary<MessageState, int>> GetMessageStatistics();

        /// <summary>
        /// Get total count of messages matching the filter criteria.
        /// </summary>
        /// <param name="queueId">Optional queue ID to filter by. If null, counts messages from all queues.</param>
        /// <param name="processedOnly">Optional filter: true for processed only, false for unprocessed only, null for all.</param>
        /// <returns>Total count of matching messages.</returns>
        ValueTask<int> GetMessageCount(long? queueId = null, bool? processedOnly = null);
    }
}
