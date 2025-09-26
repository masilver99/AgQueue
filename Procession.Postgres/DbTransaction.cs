// <copyright file="DbTransaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>
using Npgsql;
using AgQueue.Server.Common;

namespace AgQueue.Postgres
{
    /// <summary>
    /// Wrapper around SQLite transaction.  Used by storage classes.
    /// </summary>
    internal class DbTransaction : IStorageTransaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="connection">SQLite connection.</param>
        public DbTransaction(NpgsqlConnection connection)
        {
            this.NpgsqlTransaction = connection.BeginTransaction();
        }

        /// <summary>
        /// Gets the internal SQLite transaction.
        /// </summary>
        internal NpgsqlTransaction NpgsqlTransaction { get; }

        /// <inheritdoc/>
        public void Commit()
        {
            this.NpgsqlTransaction.Commit();
            // this should also close out the connection
        }

        /// <inheritdoc/>
        public void Rollback()
        {
            this.NpgsqlTransaction.Rollback();
        }
    }
}
