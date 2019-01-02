using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Library
{
    public class InternalApi : IDisposable
    {

        private long _transId = 0;
        internal long _messageId = 0;
        private long _queueId = 0;

        private IStorage _storage;

        private QueueList _queueList = new QueueList();

        private static readonly DateTime MaxDateTime = DateTime.MaxValue;

        static readonly Regex _queueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        //Settings
        //How long until a transcation expires and is automatically rolled back
        private readonly int _expiryTimeInMinutes = 30;

        public InternalApi(bool deleteExistingData = false)
        {
            //Setup Storage
            //TODO: We can set this by config at a later time.  Currently, only Sqlite is supported
            _storage = new StorageSqlite();
            _storage.InitializeStorage(deleteExistingData, @"Data Source=SqlLite.db;");

            //Get starting Id's.  These are used to increment primary keys.
            _transId = _storage.GetMaxTransId();
            _messageId = _storage.GetMaxMessageId();
            _queueId = _storage.GetMaxQueueId();

            //Cache queue names/ids
            _queueList.Reload(_storage.GetFullQueueList());
        }

        internal long StartTransaction()
        {
            var newId = Interlocked.Increment(ref _transId);
            _storage.StartTransaction(newId, _expiryTimeInMinutes);
            return newId;
        }

        /// <summary>
        /// Updates the specified transaction, reseting it's timeout
        /// </summary>
        /// <param name="transId"></param>
        internal TransactionResult UpdateTransaction(long transId)
        {
            var transModel = _storage.GetTransactionById(transId);

            //Validate Transaction
            if (transModel == null)
                return TransactionResult.NotFound;
            if (!transModel.Active)
                return TransactionResult.Closed;
            if (transModel.ExpiryDateTime <= DateTime.Now)
            {
                //Took too long to run transaction, so now we have to rollback, just in case :-(
                RollbackTransaction(transId);
                return TransactionResult.Expired;
            }

            //Transaction is valid, so let's update it
            _storage.UpdateTransaction(transId, _expiryTimeInMinutes);
            return TransactionResult.Success;
        }

        internal TransactionResult CommitTransaction(long transId)
        {
            var storageTransaction = _storage.BeginStorageTransaction();

            //Check if transaction has expired
            var transModel = _storage.GetTransactionById(transId, storageTransaction);
            if (transModel == null)
                return TransactionResult.NotFound;
            if (!transModel.Active)
                return TransactionResult.Closed;
            if (transModel.ExpiryDateTime <= DateTime.Now)
            {
                //Took too long to run transaction, so now we have to rollback :-(
                RollbackTransaction(transId);
                return TransactionResult.Expired;
            }

            var commitDateTime = DateTime.Now;

            //Updated newly added messages
            _storage.CommitAddedMessages(transId, storageTransaction);

            //Update newly completed messages
            _storage.CommitPulledMessages(transId, storageTransaction, commitDateTime);

            //Update Transaction record
            _storage.CommitMessageTransaction(transId, storageTransaction, commitDateTime);

            storageTransaction.Commit();
            return TransactionResult.Success;
        }

        internal void RollbackTransaction(long transId)
        {
            var storageTrans = _storage.BeginStorageTransaction();
            var closeDateTime = DateTime.Now;

            //Close the transaction
            _storage.CloseTransaction(transId, storageTrans, closeDateTime);

            //Removed messages added during the transaction
            _storage.DeleteNewMessagesByTransId(transId, storageTrans);

            //Check if open messages are at the retry threshold, if so , mark as closed
            _storage.CloseRetriedMessages(transId, storageTrans);

            //Check if open messages are past the expiry date, if so mark as such
            _storage.ExpireOlderMessages(transId, storageTrans, closeDateTime);

            //All other records, increment retry count, mark record as active and ready to be pulled again
            _storage.UpdateRetriesOnRollbackedMessages(transId, storageTrans);

            storageTrans.Commit();
        }


        #region Queues

        private void ValidateQueueName(in string queueName)
        {
            if (!_queueNameRegex.IsMatch(queueName))
                throw new ArgumentException("Queue name can only contain a-Z, 0-9, ., -, or _");
        }

        public long CreateQueue(String name)
        {
            //validation
            if (name.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            ValidateQueueName(in name);
            if (_queueList.ContainsKey(name))
                throw new Exception("Queue already exists");

            //Add new queue
            var nextId = Interlocked.Increment(ref _queueId);
            _storage.AddQueue(nextId, name);
            var queueModel = new WorkQueueModel() {Id = nextId, Name = name};
            _queueList.Add(queueModel);

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
        public void DeleteQueue(String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            if (!_queueList.TryGetQueueId(fixedName, out var id))
                throw new Exception("Queue not found");
            var trans = _storage.BeginStorageTransaction();

            //TODO: Rollback queue transactions that were being used in message for this queue
            //TODO: Delete Messages

            //TODO: Delete actual queue table

            //Delete From Queue Table
            _storage.DeleteQueue(id, trans);
            trans.Commit();
            _queueList.Delete(fixedName);
        }

        #endregion

        public void Dispose()
        {
            _storage.Dispose();
        }

        internal WorkQueueModel QueueModel { get; set; }

        public void AddMessage(Int64 transId, Int64 queueId, Object message, string metaData, int priority = 0,
            int maxRetries = 3, DateTime? rawExpiryDateTime = null, int correlation = 0, string groupName = null)
        {

            var addDateTime = DateTime.Now;
            DateTime expiryDateTime = rawExpiryDateTime ?? DateTime.MaxValue;
            var nextId = Interlocked.Increment(ref _messageId);
            var compressedMessage = MessagePack.LZ4MessagePackSerializer.Serialize(message);
            _storage.AddMessage(transId, null, nextId, queueId, compressedMessage, addDateTime, metaData, priority,
                maxRetries,
                expiryDateTime, correlation, groupName);
        }

    }

    internal class TransactionModel
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime ExpiryDateTime { get; set; }
    }

    internal class MessageModel
    {
        /// <summary>
        /// Generated unique message id
        /// </summary>
        public int Id { get; set; }
        public int QueueId { get; set; }
        public int TransactionId { get; set; }
        public int TransactionAction { get; set; }
        public DateTime AddDateTime { get; set; }
        public DateTime CloseDateTime { get; set; }
        public int Priority { get; set; }

        /// <summary>
        /// Numerber of attempts to have message processed, i.e. commited
        /// </summary>
        public int MaxRetries { get; set; }
        /// <summary>
        /// Number of Rollbacks or timeouts before the message expires
        /// </summary>
        public int Retries { get; set; } = 0;
        /// <summary>
        /// DateTime the message will expire
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        public int CorrelationId { get; set; }
        public string Group { get; set; }

        /// <summary>
        /// Actual message data 
        /// </summary>
        public byte[] Data { get; set; }
    }

    internal class WorkQueueModel
    {
        public Int64 Id { get; set; }
        public string Name { get; set; }
    }

    internal sealed class TransactionAction
    {
        public static readonly TransactionAction Add = new TransactionAction("Add", 0);
        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        public readonly string Name;
        public readonly int Value;

        private TransactionAction(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    internal sealed class MessageState
    {
        public static readonly MessageState Active = new MessageState("Active", 0);
        public static readonly MessageState InTransaction = new MessageState("InTransaction", 1);
        public static readonly MessageState Processed = new MessageState("Processed", 2);
        public static readonly MessageState Expired = new MessageState("Expired", 3);
        public static readonly MessageState RetryExceeded = new MessageState("RetryExceeded", 4);

        public readonly string Name;
        public readonly int Value;

        private MessageState(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    //This may need to be in the public API
    public enum TransactionResult
    {
        Success = 0, 
        Expired = 1,
        Closed = 2,
        NotFound = 3
    }
}
