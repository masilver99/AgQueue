// <copyright file="MessageModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a Queue Message.
    /// </summary>
    internal class Message
    {
        /// <summary>
        /// Gets generated unique message id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the id of the queue.
        /// </summary>
        public int QueueId { get; }

        /// <summary>
        /// Gets the id of the queue transaction.
        /// </summary>
        public int TransactionId { get; }

        /// <summary>
        /// Gets the transaction action. i.e. was this message added or pulled in the transaction.
        /// </summary>
        public int TransactionAction { get; }

        /// <summary>
        /// Gets the datetime the message was added.
        /// </summary>
        public DateTime AddDateTime { get; }

        /// <summary>
        /// Gets the datetime the message was closed, i.e. processed or cancelled or expired.
        /// </summary>
        public DateTime CloseDateTime { get; }

        /// <summary>
        /// Gets the priority of the message.  Lower is higher priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets number of attempts to have message processed, i.e. commited.
        /// </summary>
        public int MaxRetries { get; }

        /// <summary>
        /// Gets the number of rollbacks or timeouts before the message expires.
        /// </summary>
        public int Retries { get; } = 0;

        /// <summary>
        /// Gets DateTime the message will expire.
        /// </summary>
        public DateTime ExpiryDate { get; }

        /// <summary>
        /// Gets the interger correlation id, used by calling application.
        /// </summary>
        public int CorrelationId { get; }

        /// <summary>
        /// Gets string group name.  Used by external application for grouping purposes.
        /// </summary>
        public string? Group { get; }

        /// <summary>
        /// Gets actual message data.
        /// </summary>
        public byte[]? Payload { get; }
    }
}
