namespace NWorkQueue.Library
{
    using System;
    using System.Threading;
    using NWorkQueue.Common;

    public class Transaction
    {
        // Settings
        // How long until a transcation expires and is automatically rolled back
        private readonly int expiryTimeInMinutes = 30;

        private long transId = 0;

        private IStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="storage">An IStorage interface</param>
        internal Transaction(IStorage storage)
        {
            this.storage = storage;

            // Get starting Id.  used to increment primary keys.
            this.transId = this.storage.GetMaxTransId();
        }

        internal long StartTransaction()
        {
            var newId = Interlocked.Increment(ref this.transId);
            this.storage.StartTransaction(newId, _expiryTimeInMinutes);
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
            this.storage.UpdateTransaction(transId, _expiryTimeInMinutes);
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

    }
}
