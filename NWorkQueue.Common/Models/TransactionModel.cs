// <copyright file="TransactionModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    using System;

    /// <summary>
    /// Represents a Queue Transaction
    /// </summary>
    public class TransactionModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionModel"/> class.
        /// </summary>
        /// <param name="id">primary key</param>
        /// <param name="active">Is transaction active</param>
        /// <param name="createDateTime">Datetime transaction was created</param>
        /// <param name="expiryDateTime">Datetime transaction will expire</param>
        public TransactionModel(long id, bool active, DateTime createDateTime, DateTime expiryDateTime)
        {
            this.Id = id;
            this.Active = active;
            this.CreateDateTime = createDateTime;
            this.ExpiryDateTime = expiryDateTime;
        }

        /// <summary>
        /// Gets the unique ID for a transaction
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Gets a value indicating whether gets the transaction's active state
        /// Is the transaction active?  e.g. has it been committed or rolled back
        /// </summary>
        public bool Active { get; }

        /// <summary>
        /// Gets the date and time the transaction was creted
        /// </summary>
        public DateTime CreateDateTime { get; }

        /// <summary>
        /// Gets the date and time the transaction will expire. e.g. after this datetime, the transaction will automatically rollback
        /// </summary>
        public DateTime ExpiryDateTime { get; }
    }
}
