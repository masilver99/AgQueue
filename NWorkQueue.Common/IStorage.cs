namespace NWorkQueue.Common
{
    using System;
    using System.Collections.Generic;

    public interface IStorage : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deleteExistingData"></param>
        /// <param name="settings">Could be connection string, could be empty, could be json settings.  Depends on the underlying storage class</param>
        void InitializeStorage(bool deleteExistingData, string settings);

        long GetMaxTransId();

        long GetMaxMessageId();

        long GetMaxQueueId();

        void StartTransaction(long newId, int expiryTimeInMinutes);

        void UpdateTransaction(long transId, int expiryTimeInMinutes);

        TransactionModel GetTransactionById(long transId, IStorageTransaction storageTrans);

        TransactionModel GetTransactionById(long transId);

        // Not all Storage classes will have internal transactions, so this can return a dummy class
        IStorageTransaction BeginStorageTransaction();

        void CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime);

        void DeleteNewMessagesByTransId(long transId, IStorageTransaction storageTrans);

        void CloseRetriedMessages(long transId, IStorageTransaction storageTrans);

        void ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime);

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
        /// <param name="name">Name of the queue to lookup</param>
        /// <returns></returns>
        long? GetQueueId(string name);

        bool DoesQueueExist(long id);

        void DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans);
    }
}
