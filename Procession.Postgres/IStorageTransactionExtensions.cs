// <copyright file="IStorageTransactionExtensions.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using Procession.Server.Common;

namespace Procession.Postgres
{
    /// <summary>
    /// Extensions on the IStorageTransaction interface.
    /// </summary>
    internal static class IStorageTransactionExtensions
    {
        /// <summary>
        /// Converts the IStorageTransaction to a NpgsqlTransaction.
        /// </summary>
        /// <param name="iTrans">The IStorageTransaction to convert.</param>
        /// <returns>SqliteTransaction.</returns>
        public static NpgsqlTransaction NpgsqlTransaction(this IStorageTransaction iTrans)
        {
            return (iTrans as DbTransaction).NpgsqlTransaction;
        }
    }
}
