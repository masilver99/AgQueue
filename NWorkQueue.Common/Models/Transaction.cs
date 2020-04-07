// <copyright file="Transaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    using System;

    /// <summary>
    /// Represents a Queue Transaction.
    /// </summary>
    public class Transaction
    {
        public Transaction()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="id">primary key.</param>
        /// <param name="state">Is transaction active.</param>
        /// <param name="createDateTime">Datetime transaction was created.</param>
        /// <param name="expiryDateTime">Datetime transaction will expire.</param>
        /// <param name="endDateTime">Datetime the transaction was closed.</param>
        public Transaction(long id, TransactionState state, DateTime createDateTime, DateTime expiryDateTime, DateTime? endDateTime)
        {
            this.Id = id;
            this.State = state;
            this.CreateDateTime = createDateTime;
            this.ExpiryDateTime = expiryDateTime;
            this.EndDateTime = endDateTime;
        }

        /// <summary>
        /// Gets the unique ID for a transaction.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Gets a value indicating the state of the transaction.
        /// </summary>
        public TransactionState State { get; set; }

        /// <summary>
        /// Gets the date and time the transaction was created.
        /// </summary>
        public DateTime CreateDateTime { get; }

        /// <summary>
        /// Gets the date and time the transaction will expire. e.g. after this datetime, the transaction will automatically rollback.
        /// </summary>
        public DateTime ExpiryDateTime { get; }

        /// <summary>
        /// Gets the date and time the transaction was closed, null if not closed.
        /// </summary>
        public DateTime? EndDateTime { get; }
    }
}
