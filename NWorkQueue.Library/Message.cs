// <copyright file="Message.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Library
{
    using System;
    using System.Threading;
    using NWorkQueue.Common;

    /// <summary>
    /// APIS's for accessing and manging queue messages
    /// </summary>
    public class Message
    {
        private readonly IStorage storage;

        private long currMessageId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="storage">Storage library</param>
        internal Message(IStorage storage)
        {
            this.storage = storage;
            this.currMessageId = this.storage.GetMaxMessageId();
        }

        /// <summary>
        /// Adds a message to a queue
        /// </summary>
        /// <param name="transId">Queue Transaction id.  All messages must be added in a transaction</param>
        /// <param name="queueId">The queue id to add the message to</param>
        /// <param name="message">Message object to be serialized</param>
        /// <param name="metaData">String of optional data describing the message</param>
        /// <param name="priority">Message priority.  Lower the number, the higher the priority</param>
        /// <param name="maxRetries">How many failures before the message will be expired</param>
        /// <param name="rawExpiryDateTime">Datetime that the message will expire if it's not already been processed</param>
        /// <param name="correlation">Optional correlation id.  ID's are defined by the calling application</param>
        /// <param name="groupName" >Optional group string.  Defined by calling application</param>
        /// <returns>Message ID</returns>
        public long Add(long transId, long queueId, object message, string metaData, int priority = 0, int maxRetries = 3, DateTime? rawExpiryDateTime = null, int correlation = 0, string groupName = null)
        {
            var addDateTime = DateTime.Now;
            DateTime expiryDateTime = rawExpiryDateTime ?? DateTime.MaxValue;
            var nextId = Interlocked.Increment(ref this.currMessageId);
            var compressedMessage = MessagePack.LZ4MessagePackSerializer.Serialize(message);
            this.storage.AddMessage(transId, null, nextId, queueId, compressedMessage, addDateTime, metaData, priority, maxRetries, expiryDateTime, correlation, groupName);
            return nextId;
        }
    }
}
