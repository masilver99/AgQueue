// <copyright file="Message.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Library
{
    using System;
    using System.Threading;
    using NWorkQueue.Common;

    public class Message
    {
        private readonly IStorage storage;

        private long currMessageId = 0;

        internal Message(IStorage storage)
        {
            this.storage = storage;
            this.currMessageId = this.storage.GetMaxMessageId();
        }

        public void AddMessage(long transId, long queueId, object message, string metaData, int priority = 0,
            int maxRetries = 3, DateTime? rawExpiryDateTime = null, int correlation = 0, string groupName = null)
        {
            var addDateTime = DateTime.Now;
            DateTime expiryDateTime = rawExpiryDateTime ?? DateTime.MaxValue;
            var nextId = Interlocked.Increment(ref this.currMessageId);
            var compressedMessage = MessagePack.LZ4MessagePackSerializer.Serialize(message);
            this.storage.AddMessage(transId, null, nextId, queueId, compressedMessage, addDateTime, metaData, priority,
                maxRetries,
                expiryDateTime, correlation, groupName);
        }
    }
}
