// <copyright file="DbTransaction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace NWorkQueue.Sqlite
{
    using Microsoft.Data.Sqlite;
    using NWorkQueue.Common;

    internal class DbTransaction : IStorageTransaction
    {
        internal SqliteTransaction SqliteTransaction { get; set; }

        void IStorageTransaction.Commit()
        {
            this.SqliteTransaction.Commit();
        }

        void IStorageTransaction.Rollback()
        {
            this.SqliteTransaction.Rollback();
        }
    }
}
