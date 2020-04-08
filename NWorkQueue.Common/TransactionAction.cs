// <copyright file="TransactionAction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    /// <summary>
    /// Represents the action performed within a transaction.
    /// </summary>
    public enum TransactionAction
    {
        None = 0,

        /// <summary>
        /// Item was added within a transaction (if rolledback, delete).
        /// </summary>
        Add = 1,

        /// <summary>
        /// Item was pulled within transaction, increment retry count if rolledback.
        /// </summary>
        Pull = 2
    }
}
