// <copyright file="Message.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace AgQueue.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a Queue Message.
    /// </summary>
    public record Message
    {
        /// <summary>
        /// Gets generated unique message id.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the id of the queue.
        /// </summary>
        public int QueueId { get; init; }

        /// <summary>
        /// Gets the id of the queue transaction.
        /// </summary>
        public int TransactionId { get; init; }

        /// <summary>
        /// Gets the transaction action. i.e. was this message added or pulled in the transaction.
        /// </summary>
        public TransactionAction TransactionAction { get; init; }

        /// <summary>
        /// Gets the datetime the message was added.
        /// </summary>
        public long AddDateTime { get; init; }

        /// <summary>
        /// Gets the datetime the message was closed, i.e. processed or cancelled or expired.
        /// </summary>
        public long? CloseDateTime { get; init; }

        /// <summary>
        /// Gets the priority of the message.  Lower is higher priority.
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// Gets number of attempts to have message processed, i.e. commited.
        /// </summary>
        public int MaxAttempts { get; init; }

        /// <summary>
        /// Gets the number of rollbacks or timeouts before the message expires.
        /// </summary>
        public int Attempts { get; init; }

        /// <summary>
        /// Gets DateTime the message will expire.
        /// </summary>
        public long? ExpiryDateTime { get; init; }

        /// <summary>
        /// Gets the interger correlation id, used by calling application.
        /// </summary>
        public int CorrelationId { get; init; }

        /// <summary>
        /// Gets string group name.  Used by external application for grouping purposes.
        /// </summary>
        public string? GroupName { get; init; }

        /// <summary>
        /// Gets actual message data.
        /// </summary>
        public byte[]? Payload { get; init; }

        /// <summary>
        /// Gets Message State, i.e. Active, closed, etc.
        /// </summary>
        public MessageState MessageState { get; init; }

        /// <summary>
        /// Gets string based metadata describing the message in more detail.  Optional.
        /// </summary>
        public string Metadata { get; init; }
    }
}
