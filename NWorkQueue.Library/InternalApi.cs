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
    /// This is mostly a factory for creating Queues and Transactions
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

            // this.Message = new Message(this.storage);
            // this.Transaction = new Transaction(this.storage);
            // this.Queue = new Queue(this.storage);
        }

        // Below is a set of factories.  All of these methods return a Queue, Transaction or Message

        /// <summary>
        /// Creates a new queue. An exception is thrown is queue already exists.
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <returns>A Queue object</returns>
        public Queue CreateQueue(string queueName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a Queue object by name
        /// </summary>
        /// <param name="queueName">The name of the queue to return</param>
        /// <param name="autoCreate">If the name doesn't exist, create it otherwise throw an exception</param>
        /// <returns>A Queue object</returns>
        public Queue GetQueueByName(string queueName, bool autoCreate)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Returns a Queue object based on a queue ID
        /// </summary>
        /// <param name="queueId">The Queue ID to lookup</param>
        /// <returns>A Queue object</returns>
        public Queue GetQueueById(long queueId)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets Queue related APIs
        /// </summary>
        // public Queue Queue { get; }

        /// <summary>
        /// Gets Transaction related APIs
        /// </summary>
        // public Transaction Transaction { get; }

        /// <summary>
        /// Gets Message related APIs
        /// </summary>
        // public Message Message { get; }

        /// <summary>
        /// Disposes of storage resources
        /// </summary>
        public void Dispose()
        {
            this.storage.Dispose();
        }
    }
}
