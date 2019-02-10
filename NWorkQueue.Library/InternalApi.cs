// <copyright file="InternalApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using NWorkQueue.Common;
using NWorkQueue.Sqlite;

[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Library
{
    /// <summary>
    /// Starting point for accessing all queue related APIS
    /// </summary>
    public class InternalApi : IDisposable
    {
        private static readonly DateTime MaxDateTime = DateTime.MaxValue;

        private readonly IStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalApi"/> class.
        /// </summary>
        /// <param name="deleteExistingData">Deletes all Queues, Messages and Transactions</param>
        public InternalApi(bool deleteExistingData = false)
        {
            // Setup Storage
            // TODO: We can set this by config at a later time.  Currently, only Sqlite is supported
            this.storage = new StorageSqlite();
            this.storage.InitializeStorage(deleteExistingData, @"Data Source=SqlLite.db;");

            this.Message = new Message(this.storage);
            this.Transaction = new Transaction(this.storage);
            this.Queue = new Queue(this.storage);
        }

        /// <summary>
        /// Gets Queue related APIs
        /// </summary>
        public Queue Queue { get; }

        /// <summary>
        /// Gets Transaction related APIs
        /// </summary>
        public Transaction Transaction { get; }

        /// <summary>
        /// Gets Message related APIs
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// Disposes of storage resources
        /// </summary>
        public void Dispose()
        {
            this.storage.Dispose();
        }
    }
}
