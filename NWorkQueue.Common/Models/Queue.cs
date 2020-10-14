// <copyright file="Queue.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common.Models
{
    /// <summary>
    /// Represents a Queue.
    /// </summary>
    public record Queue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="id">queue Id.</param>
        /// <param name="name">queue name.</param>
        /*
        public Queue(long id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        */
        /// <summary>
        /// Gets the unique queue id.
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        public string Name { get; init; }
    }
}
