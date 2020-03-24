// <copyright file="InternalApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NWorkQueue.Common;
using NWorkQueue.Server.Common.Extensions;
using NWorkQueue.Server.Common.Models;

[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Server.Common
{
    /// <summary>
    /// Starting point for accessing all queue related APIS
    /// This is mostly a factory for creating Queues and Transactions.
    /// </summary>
    /// <remarks>
    /// Exceptions are not used unless there is an exceptional condition.  For example, if an items doesn't exist or a param is invalid, 
    /// this is handled without an exception.  This is mostly for speed and simplicity with the gRPC interface.
    /// </remarks>
    public class InternalApi : IDisposable
    {
        private static readonly DateTime MaxDateTime = DateTime.MaxValue;

        private readonly IStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalApi"/> class.
        /// </summary>
        public InternalApi(IStorage storage)
        {
            // Setup Storage
            this.storage = storage;
        }

        /// <summary>
        /// Creates a new queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <returns>A Queue info object.  Note: the queueName is the stadnardized name.</returns>
        public async ValueTask<QueueInfo> CreateQueue(string queueName)
        {
            var fixedName = queueName.StandardizeQueueName();

            var queueInfo = await this.GetQueueInfoByName(fixedName);

            // Check if Queue already exists
            if (queueInfo != null)
            {
                throw new Exception($"Queue name already exists. {queueName}");
            }

            return new QueueInfo { Id = await this.storage.AddQueue(fixedName), Name = queueName };
        }

        /// <summary>
        /// Delete a queue and all messages in the queue.
        /// Throws an exception if Queue doesn't exist
        /// </summary>
        /// <param name="queueName">Name of the queue to delete.</param>
        public async void DeleteQueue(string queueName)
        {
            var fixedName = queueName.StandardizeQueueName();
            if (fixedName.Length == 0)
            {
                throw new ArgumentNullException("Queue name is empty");
            }

            var queueInfo = await this.GetQueueInfoByName(fixedName);

            // QueueInfo should never be null if IsSuccess = true
            if (queueInfo == null)
            {
                throw new Exception($"Queue name not found: {queueName}.");
            }
        }

        public async ValueTask<QueueInfo?> GetQueueInfoById(long queueId)
        {
            return await this.storage.GetQueueInfoById(queueId);
        }

        public async ValueTask<QueueInfo?> GetQueueInfoByName(string queueName)
        {
            return await this.storage.GetQueueInfoByName(queueName.StandardizeQueueName());
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue.
        /// </summary>
        /// <param name="queueId">Queue id.</param>
        public async ValueTask DeleteQueue(long queueId)
        {
            var queueInfo = await this.GetQueueInfoById(queueId);

            if (queueInfo == null)
            {
                throw new Exception($"Queue ID not found: {queueId}.");
            }

            await this.storage.DeleteQueue(queueId);
        }

        /// <summary>
        /// Gets Queue related APIs.
        /// </summary>
        // public Queue Queue { get; }

        /// <summary>
        /// Gets Transaction related APIs
        /// </summary>
        // public Transaction Transaction { get; }

        /// <summary>
        /// Gets Message related APIs
        /// </summary>
        // public Message Message { get; }

        /// <summary>
        /// Disposes of storage resources
        /// </summary>
        public void Dispose()
        {
            ///Need to add OnDsiapose action in case some storage services require disposal
        }

        public async ValueTask InitializeStorage(bool deleteExistingData)
        {
            await this.storage.InitializeStorage(deleteExistingData);
        }

        internal Transaction CreateTransaction()
        {
            throw new NotImplementedException();
        }
    }
}
