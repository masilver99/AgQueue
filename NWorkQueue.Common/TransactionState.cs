// <copyright file="TransactionState.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Common
{
    /// <summary>
    /// The current state of a transaction.
    /// </summary>
    public sealed class TransactionState
    {
        /// <summary>
        /// This should never occur.  It means there is a serious bug.
        /// </summary>
        public static readonly TransactionState Unknown = new TransactionState("Unknown", 0);

        /// <summary>
        /// Transaction is active.
        /// </summary>
        public static readonly TransactionState Active = new TransactionState("Active", 1);

        /// <summary>
        /// Transaction has been committed by user.
        /// </summary>
        public static readonly TransactionState Commited = new TransactionState("Commited", 2);

        /// <summary>
        /// Transaction was rolled back by user.
        /// </summary>
        public static readonly TransactionState RolledBack = new TransactionState("RolledBack", 3);

        /// <summary>
        /// Transaction was automatically expired due to timeout.
        /// </summary>
        public static readonly TransactionState Expired = new TransactionState("Expired", 4);

        private TransactionState(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets transaction state name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets transaction state value / id.
        /// </summary>
        public int Value { get; }
    }
}
