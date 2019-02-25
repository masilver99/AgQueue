// <copyright file="Transaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Library
{
    using System;
    using System.Threading;
    using NWorkQueue.Common;

    /// <summary>
    /// Represents a Queue Transaction.  Most message functions require a transaction
    /// </summary>
    public class Transaction
    {
        // Settings
        // How long until a transcation expires and is automatically rolled back
        private readonly int expiryTimeInMinutes = 30;

        private readonly IStorage storage;

        private long currTransId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="storage">An IStorage interface</param>
        internal Transaction(IStorage storage)
        {
            this.storage = storage;

            // Get starting Id.  used to increment primary keys.
            this.currTransId = this.storage.GetMaxTransactionId();
        }

        /// <summary>
        /// Start Queue Transaction
        /// </summary>
        /// <returns>Queue Transaction id of the new transaction</returns>
        internal long Start()
        {
            var newId = Interlocked.Increment(ref this.currTransId);
            this.storage.StartTransaction(newId, this.expiryTimeInMinutes);
            return newId;
        }

        /// <summary>
        /// Updates the specified transaction, reseting it's timeout
        /// </summary>
        /// <param name="transId">Queue Transaction id</param>
        /// <returns>Enum detailing if update was successul</returns>
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
            this.storage.ExtendTransaction(transId, this.expiryTimeInMinutes);
            return TransactionResult.Success;
        }

        /// <summary>
        /// Returns the message count of messages in a specific transaction. Results are unknown for closed committed or rolledback transactions.
        /// </summary>
        /// <param name="transId">Transaction id</param>
        /// <returns>Message count</returns>
        public long GetMessageCount(long transId)
        {
            return this.storage.GetMessageCount(queueId);
        }

        /// <summary>
        /// Commits the Queue Transaction
        /// </summary>
        /// <param name="transId">Queue transaction id</param>
        /// <returns>Was the commit successful</returns>
        public TransactionResult Commit(long transId)
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

        /// <summary>
        /// Rollsback the Queue transaction and resets the messages states or doesn't add messages
        /// </summary>
        /// <param name="transId">Queue transaction id</param>
        public void RollbackTransaction(long transId)
        {
            var storageTrans = this.storage.BeginStorageTransaction();
            var closeDateTime = DateTime.Now;

            // Close the transaction
            this.storage.CloseTransaction(transId, storageTrans, closeDateTime);

            // Removed messages added during the transaction
            this.storage.DeleteMessagesByTransId(transId, storageTrans);

            // Check if open messages are at the retry threshold, if so , mark as closed
            this.storage.CloseRetriedMessages(transId, storageTrans, closeDateTime);

            // Check if open messages are past the expiry date, if so mark as such
            this.storage.ExpireOlderMessages(transId, storageTrans, closeDateTime, closeDateTime);

            // All other records, increment retry count, mark record as active and ready to be pulled again
            this.storage.UpdateRetriesOnRollbackedMessages(transId, storageTrans);

            storageTrans.Commit();
        }
    }
}
