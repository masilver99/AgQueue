// <copyright file="IStorageTransaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace AgQueue.Server.Common
{
    /// <summary>
    /// Represents a transaction used by the storage (usually a database transaction).
    /// </summary>
    public interface IStorageTransaction
    {
        /// <summary>
        /// Commits the transaction, usually this reprents a database transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollsback the transaction, usually this reprents a database transaction.
        /// </summary>
        void Rollback();
    }
}
