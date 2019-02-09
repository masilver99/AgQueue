namespace NWorkQueue.Library
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using NWorkQueue.Common;

    public class Queue
    {
        private long queueId = 0;

        private IStorage storage;

        static readonly Regex QueueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        internal Queue(IStorage storage)
        {
            this.storage = storage;

            // Get starting Id. These are used to increment primary keys.
            this.queueId = this.storage.GetMaxQueueId();
        }

        private void ValidateQueueName(string queueName)
        {
            if (!QueueNameRegex.IsMatch(queueName))
            {
                throw new ArgumentException("Queue name can only contain a-Z, 0-9, ., -, or _");
            }
        }

        /// <summary>
        /// Creates a new queue. Queue cannot already exist
        /// </summary>
        /// <param name="name"></param>
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
            var nextId = Interlocked.Increment(ref this.queueId);
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
        public void DeleteQueue(String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            var id = this.storage.GetQueueId(fixedName);

            if (!id.HasValue)
                throw new Exception("Queue not found");
            DeleteQueue(id.Value);
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue
        /// </summary>
        /// <param name="queueId"></param>
        public void DeleteQueue(long queueId)
        {
            // Throw exception if queue does not exist
            if (!this.storage.DoesQueueExist(queueId))
                throw new Exception("Queue not found");

            var trans = this.storage.BeginStorageTransaction();
            try
            {
                //TODO: Rollback queue transactions that were being used in message for this queue
                //UpdateTransaction Set Active = false


                //TODO: Delete Messages
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
    }
}
