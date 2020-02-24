// <copyright file="QueueModel.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    /// <summary>
    /// Represents a Queue.
    /// </summary>
    public class QueueModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueModel"/> class.
        /// </summary>
        /// <param name="id">queue Id.</param>
        /// <param name="name">queue name.</param>
        public QueueModel(long id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        /// <summary>
        /// Gets the unique queue id.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        public string Name { get; }
    }
}
