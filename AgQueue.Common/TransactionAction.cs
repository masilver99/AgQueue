﻿// <copyright file="TransactionAction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace AgQueue.Common
{
    /// <summary>
    /// Represents the action performed within a transaction.
    /// </summary>
    public enum TransactionAction
    {
        /// <summary>
        /// This should never happen.
        /// </summary>
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
