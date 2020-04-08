// <copyright file="Message.cs" company="Michael Silver">
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
    public class Message
    {
        public Message()
        {

        }

        /// <summary>
        /// Gets generated unique message id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets the id of the queue.
        /// </summary>
        public int QueueId { get; set; }

        /// <summary>
        /// Gets the id of the queue transaction.
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Gets the transaction action. i.e. was this message added or pulled in the transaction.
        /// </summary>
        public TransactionAction TransactionAction { get; set; }

        /// <summary>
        /// Gets the datetime the message was added.
        /// </summary>
        public long AddDateTime { get; set; }

        /// <summary>
        /// Gets the datetime the message was closed, i.e. processed or cancelled or expired.
        /// </summary>
        public long? CloseDateTime { get; set; }

        /// <summary>
        /// Gets the priority of the message.  Lower is higher priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets number of attempts to have message processed, i.e. commited.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets the number of rollbacks or timeouts before the message expires.
        /// </summary>
        public int Retries { get; set; } = 0;

        /// <summary>
        /// Gets DateTime the message will expire.
        /// </summary>
        public long? ExpiryDateTime { get; set; }

        /// <summary>
        /// Gets the interger correlation id, used by calling application.
        /// </summary>
        public int CorrelationId { get; set; }

        /// <summary>
        /// Gets string group name.  Used by external application for grouping purposes.
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// Gets actual message data.
        /// </summary>
        public byte[]? Payload { get; set; }

        /// <summary>
        /// Gets Message State, i.e. Active, closed, etc.
        /// </summary>
        public MessageState MessageState { get; set; } = MessageState.InTransaction;

        /// <summary>
        /// Gets string based metadata describing the message in more detail.  Optional.
        /// </summary>
        public string Metadata { get; } = string.Empty;
    }
}
