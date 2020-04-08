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
        /// Gets the unique ID for a transaction.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets a value indicating the state of the transaction.
        /// </summary>
        public TransactionState State { get; set; }

        /// <summary>
        /// Gets the date and time the transaction was created.
        /// </summary>
        public long CreateDateTime { get; set;  }

        /// <summary>
        /// Gets the date and time the transaction will expire. e.g. after this datetime, the transaction will automatically rollback.
        /// </summary>
        public long ExpiryDateTime { get; set; }

        /// <summary>
        /// Gets the date and time the transaction was closed, null if not closed.
        /// </summary>
        public long? EndDateTime { get; set; }
    }
}
