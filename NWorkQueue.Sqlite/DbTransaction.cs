// <copyright file="DbTransaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>
using Microsoft.Data.Sqlite;
using NWorkQueue.Server.Common;
//using System.Data.SQLite;

namespace NWorkQueue.Sqlite
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
        public DbTransaction(SqliteConnection connection)
        {
            this.SqliteTransaction = connection.BeginTransaction();
        }

        /// <summary>
        /// Gets the internal SQLite transaction.
        /// </summary>
        internal SqliteTransaction SqliteTransaction { get; }

        /// <inheritdoc/>
        public void Commit()
        {
            this.SqliteTransaction.Commit();
            // this should also close out the connection
        }

        /// <inheritdoc/>
        public void Rollback()
        {
            this.SqliteTransaction.Rollback();
        }
    }
}
