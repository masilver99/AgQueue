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

        internal WorkQueueModel QueueModel { get; set; }

        public void Dispose()
        {
            this.storage.Dispose();
        }
    }

    internal class TransactionModel
    {
        public int Id { get; set; }

        public bool Active { get; set; }

        public DateTime CreateDateTime { get; set; }

        public DateTime ExpiryDateTime { get; set; }
    }

    internal class MessageModel
    {
        /// <summary>
        /// Generated unique message id
        /// </summary>
        public int Id { get; set; }

        public int QueueId { get; set; }

        public int TransactionId { get; set; }

        public int TransactionAction { get; set; }

        public DateTime AddDateTime { get; set; }

        public DateTime CloseDateTime { get; set; }

        public int Priority { get; set; }

        /// <summary>
        /// Numerber of attempts to have message processed, i.e. commited
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Number of Rollbacks or timeouts before the message expires
        /// </summary>
        public int Retries { get; set; } = 0;

        /// <summary>
        /// DateTime the message will expire
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        public int CorrelationId { get; set; }

        public string Group { get; set; }

        /// <summary>
        /// Actual message data
        /// </summary>
        public byte[] Data { get; set; }
    }

    internal class WorkQueueModel
    {
        public long Id { get; set; }

        public string Name { get; set; }
    }

    internal sealed class TransactionAction
    {
        public static readonly TransactionAction Add = new TransactionAction("Add", 0);

        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        private TransactionAction(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; }

        public int Value { get; }
    }

    internal sealed class MessageState
    {
        /// <summary>
        /// Active means the message is live and can be pulled from the queue
        /// </summary>
        public static readonly MessageState Active = new MessageState("Active", 0);
        /// <summary>
        /// InTransaction means the message is currently tied to a transaction, either during insert of during processing.  IOW, this message is currently being inserted or pulled from the queue.
        /// </summary>
        public static readonly MessageState InTransaction = new MessageState("InTransaction", 1);
        /// <summary>
        /// This message has been processed and will not be pulled
        /// </summary>
        public static readonly MessageState Processed = new MessageState("Processed", 2);
        /// <summary>
        /// Message has expired and will not be pulled
        /// </summary>
        public static readonly MessageState Expired = new MessageState("Expired", 3);
        /// <summary>
        /// Message retry limit has been reached and message will no longer be pulled
        /// </summary>
        public static readonly MessageState RetryExceeded = new MessageState("RetryExceeded", 4);

        private MessageState(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; }

        public int Value { get; };
    }

    // This may need to be in the public API
    public enum TransactionResult
    {
        Success = 0,
        Expired = 1,
        Closed = 2,
        NotFound = 3
    }
}
