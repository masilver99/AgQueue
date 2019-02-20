// <copyright file="DbTransaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Sqlite
{
    using Microsoft.Data.Sqlite;
    using NWorkQueue.Common;

    internal class DbTransaction : IStorageTransaction
    {
        internal SqliteTransaction SqliteTransaction { get; set; }

        /// <inheritdoc/>
        public void Commit()
        {
            this.SqliteTransaction.Commit();
        }

        /// <inheritdoc/>
        public void Rollback()
        {
            this.SqliteTransaction.Rollback();
        }
    }
}
