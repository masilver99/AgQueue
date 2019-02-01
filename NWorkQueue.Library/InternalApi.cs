using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;
using NWorkQueue.Sqlite;

[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Library
{
    public class InternalApi : IDisposable
    {
        internal long _messageId = 0;

        private readonly IStorage storage;

        private long _transId = 0;

        private long _queueId = 0;

        private static readonly DateTime MaxDateTime = DateTime.MaxValue;

        static readonly Regex _queueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        // Settings
        // How long until a transcation expires and is automatically rolled back
        private readonly int _expiryTimeInMinutes = 30;

        public InternalApi(bool deleteExistingData = false)
        {
            // Setup Storage
            // TODO: We can set this by config at a later time.  Currently, only Sqlite is supported
            this.storage = new StorageSqlite();
            this.storage.InitializeStorage(deleteExistingData, @"Data Source=SqlLite.db;");

            // Get starting Id's.  These are used to increment primary keys.
            this._transId = this.storage.GetMaxTransId();
            this._messageId = this.storage.GetMaxMessageId();
            this._queueId = this.storage.GetMaxQueueId();
        }

        internal long StartTransaction()
        {
            var newId = Interlocked.Increment(ref this._transId);
            this.storage.StartTransaction(newId, this._expiryTimeInMinutes);
            return newId;
        }

        /// <summary>
        /// Updates the specified transaction, reseting it's timeout
        /// </summary>
        /// <param name="transId"></param>
        internal TransactionResult UpdateTransaction(long transId)
        {
            var transModel = this.storage.GetTransactionById(transId);

            // Validate Transaction
            if (transModel == null)
            {
                return TransactionResult.NotFound;
            }

            if (!transModel.Active)
            {
                return TransactionResult.Closed;
            }

            if (transModel.ExpiryDateTime <= DateTime.Now)
            {
                // Took too long to run transaction, so now we have to rollback, just in case :-(
                this.RollbackTransaction(transId);
                return TransactionResult.Expired;
            }

            // Transaction is valid, so let's update it
            this.storage.UpdateTransaction(transId, this._expiryTimeInMinutes);
            return TransactionResult.Success;
        }

        /// <summary>
        /// Returns the message count for available messages (messages in a transaction will not be included)
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        internal long GetMessageCount(long queue)
        {
            return this.storage.GetMessageCount(queue);
        }

        internal TransactionResult CommitTransaction(long transId)
        {
            var storageTransaction = this.storage.BeginStorageTransaction();

            // Check if transaction has expired
            var transModel = this.storage.GetTransactionById(transId, storageTransaction);
            if (transModel == null)
            {
                return TransactionResult.NotFound;
            }

            if (!transModel.Active)
            {
                return TransactionResult.Closed;
            }

            if (transModel.ExpiryDateTime <= DateTime.Now)
            {
                // Took too long to run transaction, so now we have to rollback :-(
                this.RollbackTransaction(transId);
                return TransactionResult.Expired;
            }

            var commitDateTime = DateTime.Now;

            // Updated newly added messages
            this.storage.CommitAddedMessages(transId, storageTransaction);

            // Update newly completed messages
            this.storage.CommitPulledMessages(transId, storageTransaction, commitDateTime);

            // Update Transaction record
            this.storage.CommitMessageTransaction(transId, storageTransaction, commitDateTime);

            storageTransaction.Commit();
            return TransactionResult.Success;
        }

        internal void RollbackTransaction(long transId)
        {
            var storageTrans = this.storage.BeginStorageTransaction();
            var closeDateTime = DateTime.Now;

            // Close the transaction
            this.storage.CloseTransaction(transId, storageTrans, closeDateTime);

            // Removed messages added during the transaction
            this.storage.DeleteNewMessagesByTransId(transId, storageTrans);

            // Check if open messages are at the retry threshold, if so , mark as closed
            this.storage.CloseRetriedMessages(transId, storageTrans);

            // Check if open messages are past the expiry date, if so mark as such
            this.storage.ExpireOlderMessages(transId, storageTrans, closeDateTime);

            // All other records, increment retry count, mark record as active and ready to be pulled again
            this.storage.UpdateRetriesOnRollbackedMessages(transId, storageTrans);

            storageTrans.Commit();
        }


        #region Queues

        private void ValidateQueueName(string queueName)
        {
            if (!_queueNameRegex.IsMatch(queueName))
            {
                throw new ArgumentException("Queue name can only contain a-Z, 0-9, ., -, or _");
            }
        }

        /// <summary>
        /// Creates a new queue. Queue cannot already exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The queue Id</returns>
        public long CreateQueue(string name)
        {
            // validation
            if (name.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            }

            this.ValidateQueueName(name);

            // Check if queue already exists
            if (this.storage.GetQueueId(name).HasValue)
            {
                throw new Exception("Queue already exists");
            }

            // Add new queue
            var nextId = Interlocked.Increment(ref _queueId);
            this.storage.AddQueue(nextId, name);

            return nextId;
        }

        /* Not sure this is needed
        public WorkQueue GetQueue(string queueName)
        {
            if (queueName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
            ValidateQueueName(in queueName);
            if (!_queueList.TryGetQueue(queueName, out WorkQueueModel workQueueModel))
                throw new Exception("Queue does not exist");
            return new WorkQueue(this, workQueueModel);
        }
        */
        public void DeleteQueue(string name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            }

            var id = this.storage.GetQueueId(fixedName);

            if (!id.HasValue)
            {
                throw new Exception("Queue not found");
            }

            this.DeleteQueue(id.Value);
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue
        /// </summary>
        /// <param name="queueId"></param>
        public void DeleteQueue(long queueId)
        {
            // Throw exception if queue does not exist
            if (!this.storage.DoesQueueExist(queueId))
            {
                throw new Exception("Queue not found");
            }

            var trans = this.storage.BeginStorageTransaction();
            try
            {
                // TODO: Rollback queue transactions that were being used in message for this queue
                // UpdateTransaction Set Active = false

                // TODO: Delete Messages
                this.storage.DeleteMessagesByQueueId(queueId, trans);

                // Delete From Queue Table
                this.storage.DeleteQueue(queueId, trans);
                trans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                trans.Rollback();
            }
        }
        #endregion

        public void Dispose()
        {
            this.storage.Dispose();
        }

        internal WorkQueueModel QueueModel { get; set; }

        public void AddMessage(long transId, long queueId, object message, string metaData, int priority = 0, int maxRetries = 3, DateTime? rawExpiryDateTime = null, int correlation = 0, string groupName = null)
        {
            var addDateTime = DateTime.Now;
            DateTime expiryDateTime = rawExpiryDateTime ?? DateTime.MaxValue;
            var nextId = Interlocked.Increment(ref this._messageId);
            var compressedMessage = MessagePack.LZ4MessagePackSerializer.Serialize(message);
            this.storage.AddMessage(transId, null, nextId, queueId, compressedMessage, addDateTime, metaData, priority, maxRetries, expiryDateTime, correlation, groupName);
        }
    }
}
