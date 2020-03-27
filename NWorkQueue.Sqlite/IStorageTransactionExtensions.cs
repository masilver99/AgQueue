using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using NWorkQueue.Server.Common;

namespace NWorkQueue.Sqlite
{
    public static class IStorageTransactionExtensions
    {
        public static SqliteTransaction SqliteTransaction(this IStorageTransaction iTrans)
        {

            return (iTrans as DbTransaction).SqliteTransaction;
        }
    }
}
