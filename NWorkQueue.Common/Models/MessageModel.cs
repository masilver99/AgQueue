// <copyright file="MessageModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

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
}
