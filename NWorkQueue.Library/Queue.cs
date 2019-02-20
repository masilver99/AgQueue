// <copyright file="Queue.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Library
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using NWorkQueue.Common;

    /// <summary>
    /// Queue API from which to manage queue
    /// </summary>
    public class Queue
    {
        private static readonly Regex QueueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        private readonly IStorage storage;

        private long currQueueId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="storage">storage engine</param>
        internal Queue(IStorage storage)
        {
            this.storage = storage;

            // Get starting Id. These are used to increment primary keys.
            this.currQueueId = this.storage.GetMaxQueueId();
        }

        /// <summary>
        /// Creates a new queue. Queue cannot already exist
        /// </summary>
        /// <param name="name">Name of queue to create</param>
        /// <returns>The queue Id</returns>
        public long CreateQueue(string name)
        {
            // validation
            if (name.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            }

            this.ValidateQueueName(name);

            // Check if queue already exists
            if (this.storage.GetQueueId(name).HasValue)
            {
                throw new Exception("Queue already exists");
            }

            // Add new queue
            var nextId = Interlocked.Increment(ref this.currQueueId);
            this.storage.AddQueue(nextId, name);

            return nextId;
        }

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
        /// Delete a queue and all messages in the queue
        /// </summary>
        /// <param name="name">Name of the queue to delete</param>
        public void DeleteQueue(string name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            }

            var id = this.storage.GetQueueId(fixedName);

            if (!id.HasValue)
            {
                throw new Exception("Queue not found");
            }

            this.DeleteQueue(id.Value);
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue
        /// </summary>
        /// <param name="queueId">Queue id</param>
        public void DeleteQueue(long queueId)
        {
            // Throw exception if queue does not exist
            if (!this.storage.DoesQueueExist(queueId))
            {
                throw new Exception("Queue not found");
            }

            var trans = this.storage.BeginStorageTransaction();
            try
            {
                // TODO: Rollback queue transactions that were being used in message for this queue
                // ExtendTransaction Set Active = false

                // TODO: Delete Messages
                this.storage.DeleteMessagesByQueueId(queueId, trans);

                // Delete From Queue Table
                this.storage.DeleteQueue(queueId, trans);
                trans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                trans.Rollback();
            }
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
