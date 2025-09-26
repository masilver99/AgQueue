// <copyright file="IStorageTransactionExtensions.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
//using System.Data.SQLite;
using System.Text;
using Microsoft.Data.Sqlite;
using Procession.Server.Common;

namespace Procession.Sqlite
{
    /// <summary>
    /// Extensions on the IStorageTransaction interface.
    /// </summary>
    internal static class IStorageTransactionExtensions
    {
        /// <summary>
        /// Converts the IStorageTransaction to a SqliteTransaction.
        /// </summary>
        /// <param name="iTrans">The IStorageTransaction to convert.</param>
        /// <returns>SqliteTransaction.</returns>
        public static SqliteTransaction SqliteTransaction(this IStorageTransaction iTrans)
        {
            return (iTrans as DbTransaction).SqliteTransaction;
        }
    }
}
