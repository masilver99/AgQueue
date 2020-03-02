// <copyright file="Queue.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Library
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using MessagePack;
    using NWorkQueue.Common;

    /// <summary>
    /// Queue API from which to manage queue.
    /// </summary>
    public class Queue
    {
        private static readonly Regex QueueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        private readonly IStorage storage;

        private long currQueueId = 0;

        public long Id => this.currQueueId;

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="storage">storage engine</param>
        internal Queue(IStorage storage)
        {
            /*
            this.storage = storage;

            // Get starting Id. These are used to increment primary keys.
            this.currQueueId = this.storage.GetMaxQueueId();
        */
            }

        /// <summary>
        /// Creates a new queue. Queue cannot already exist
        /// </summary>
        /// <param name="name">Name of queue to create</param>
        /// <returns>The queue Id</returns>
        public async ValueTask<long> Create(string name)
        {
            // validation
            if (name.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            }

            this.ValidateQueueName(name);

            // Check if queue already exists
            if ((await this.storage.GetQueueId(name)).HasValue)
            {
                throw new Exception("Queue already exists");
            }

            // Add new queue
            var nextId = Interlocked.Increment(ref this.currQueueId);
            //this.storage.AddQueue(nextId, name);

            return nextId;
        }

        /// <summary>
        /// Returns the message count for available messages (messages in a transaction will not be included)
        /// </summary>
        /// <param name="queueId">Queue id</param>
        /// <returns>Message count</returns>
        /*
        public long GetMessageCount()
        {
            return this.storage.GetMessageCount(this.currQueueId);
        }
        */

        /* Not sure this is needed
        public WorkQueue GetQueue(string queueName)
        {
            if (queueName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
            ValidateQueueName(in queueName);
            if (!_queueList.TryGetQueue(queueName, out WorkQueueModel workQueueModel))
                throw new Exception("Queue does not exist");
            return new WorkQueue(this, workQueueModel);
        }
        */

        /// <summary>
        /// Adds a message to a queue.
        /// </summary>
        /// <param name="transId">Queue Transaction id.  All messages must be added in a transaction.</param>
        /// <param name="queueId">The queue id to add the message to.</param>
        /// <param name="message">Message object to be serialized.</param>
        /// <param name="metaData">String of optional data describing the message.</param>
        /// <param name="priority">Message priority.  Lower the number, the higher the priority.</param>
        /// <param name="maxRetries">How many failures before the message will be expired.</param>
        /// <param name="rawExpiryDateTime">Datetime that the message will expire if it's not already been processed.</param>
        /// <param name="correlation">Optional correlation id.  ID's are defined by the calling application.</param>
        /// <param name="groupName" >Optional group string.  Defined by calling application.</param>
        /// <returns>Message ID.</returns>
        public long AddMessage(Transaction trans, object message, string metaData, int priority = 0, int maxRetries = 3, DateTime? rawExpiryDateTime = null, int correlation = 0, string groupName = null)
        {
            throw new NotImplementedException();
            /*
            var addDateTime = DateTime.Now;
            DateTime expiryDateTime = rawExpiryDateTime ?? DateTime.MaxValue;
            var nextId = this.storage.GetMaxMessageId();
            var compressedMessage = MessagePackSerializer.Serialize(message);
            this.storage.AddMessage(trans.Id, null, nextId, this.currQueueId, compressedMessage, addDateTime, metaData, priority, maxRetries, expiryDateTime, correlation, groupName);
            return nextId;
*/
        }

        private void ValidateQueueName(string queueName)
        {
            if (!QueueNameRegex.IsMatch(queueName))
            {
                throw new ArgumentException("Queue name can only contain a-Z, 0-9, ., -, or _");
            }
        }


    }
}
