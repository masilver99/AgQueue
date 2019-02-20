// <copyright file="IStorage.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    using System;
    using NWorkQueue.Common.Models;

    /// <summary>
    /// The interface for storing and retrieving queue information from a storage mechinism, usually a database
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Called when Queue process starts.  Connections to the storage should be made here, etc.
        /// </summary>
        /// <param name="deleteExistingData">Should all existing queues and messages be deleted</param>
        /// <param name="settings">Could be connection string, could be empty, could be json settings.  Depends on the underlying storage class</param>
        void InitializeStorage(bool deleteExistingData, string settings);

        /// <summary>
        /// Get the id of the last transaction created, assuming the last ID is the largest
        /// </summary>
        /// <returns>Returns the  Transaction Id</returns>
        long GetMaxTransactionId();

        /// <summary>
        /// Get the id of the last message created, assuming the last ID is the largest
        /// </summary>
        /// <returns>Returns the max message Id</returns>
        long GetMaxMessageId();

        /// <summary>
        /// Get the id of the last queue created, assuming the last ID is the largest
        /// </summary>
        /// <returns>Returns the max queue Id</returns>
        long GetMaxQueueId();

        /// <summary>
        /// Starts a Queue transaction (not a database one)
        /// </summary>
        /// <param name="newId">The id to use as primary key</param>
        /// <param name="expiryTimeInMinutes">When the transaction will expire</param>
        void StartTransaction(long newId, int expiryTimeInMinutes);

        /// <summary>
        /// Extends the transaction's expiration datetime
        /// </summary>
        /// <param name="transId">The id of the transaction to update</param>
        /// <param name="expiryTimeInMinutes">The new expiration datetime</param>
        void ExtendTransaction(long transId, int expiryTimeInMinutes);

        /// <summary>
        /// Starts a storage (database) transaction, not a queue transaction
        /// </summary>
        /// <remarks>
        /// Not all Storage classes will have internal transactions, so this can return a dummy class that performs no actions
        /// </remarks>
        /// <returns>Returns a class represented by IStorageTransaction which can commit or rollbacl the transaction</returns>
        IStorageTransaction BeginStorageTransaction();

        /// <summary>
        /// Retrieves Queue transaction data based on the transaction id
        /// </summary>
        /// <param name="transId">Id of the tranaction to lookup</param>
        /// <param name="storageTrans">Optional Storage transaction to perform this within</param>
        /// <returns>Transaction Model</returns>
        TransactionModel GetTransactionById(long transId, IStorageTransaction storageTrans = null);

        /// <summary>
        /// Mark the Queue Transaction as closed
        /// </summary>
        /// <param name="transId">Queue Transaction to mark as closed</param>
        /// <param name="storageTrans">The db transaction to assodicate with this update</param>
        /// <param name="closeDateTime">Datetime transaction was closed</param>
        void CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime);

        /// <summary>
        /// Delete all messages by their queue transaction id
        /// </summary>
        /// <param name="transId">Id of the queue transaction</param>
        /// <param name="storageTrans">Storage Transaction</param>
        void DeleteMessagesByTransId(long transId, IStorageTransaction storageTrans);

        /// <summary>
        /// Close messages that have too many retries
        /// </summary>
        /// <param name="transId">Queue transaction id</param>
        /// <param name="storageTrans">Storage Transaction</param>
        /// <param name="closeDateTime">Datetime the message should be marked as closed</param>
        void CloseRetriedMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime);

        /// <summary>
        /// Expire messages past their expiration date/time
        /// </summary>
        /// <param name="transId">Queue transaction id</param>
        /// <param name="storageTrans">Storage Transaction</param>
        /// <param name="closeDateTime">Datetime the message should be marked as closed</param>
        /// <param name="expiryDateTime">Messages with expiry times before this will be marked closed</param>
        void ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime, DateTime expiryDateTime);

        void UpdateRetriesOnRollbackedMessages(long transId, IStorageTransaction storageTrans);

        void CommitAddedMessages(long transId, IStorageTransaction storageTrans);

        void CommitPulledMessages(long transId, IStorageTransaction storageTrans, DateTime commitDateTime);

        void CommitMessageTransaction(long transId, IStorageTransaction storageTrans, DateTime commitDateTime);

        void AddQueue(long nextId, string name);

        void DeleteQueue(long id, IStorageTransaction storageTrans);

        void AddMessage(long transId, IStorageTransaction storageTrans, long nextId, long queueId, byte[] compressedMessage, DateTime addDateTime, string metaData = "", int priority = 0, int maxRetries = 3, DateTime? expiryDateTime = null, int correlation = 0, string groupName = "");

        long GetMessageCount(long queueId);

        /// <summary>
        /// Returns the id of the Queue.  If no queue is found, returns null
        /// </summary>
        /// <remarks>
        /// This search should be case sensitive, only use LIKE with SQLite
        /// </remarks>
        /// <param name="name">Name of the queue to lookup</param>
        /// <returns>Queue ID or null if queue not found</returns>
        long? GetQueueId(string name);

        /// <summary>
        /// Does a Quque exist for the specified id
        /// </summary>
        /// <param name="id">Quque ID</param>
        /// <returns>true if quque exists</returns>
        bool DoesQueueExist(long id);

        /// <summary>
        /// Delete all messages in a specified queue
        /// </summary>
        /// <param name="queueId">Queue id</param>
        /// <param name="storageTrans">Storage transaction</param>
        void DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans);
    }
}
