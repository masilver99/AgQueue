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

        /// <summary>
        /// Returns information about the requested queue.
        /// </summary>
        /// <param name="queueId">The ID of the queue.</param>
        /// <returns>QueueInfo object. Returns null if not found.</returns>
        public async ValueTask<QueueInfo?> GetQueueInfoById(long queueId)
        {
            return await this.storage.GetQueueInfoById(queueId);
        }

        /// <summary>
        /// Returns information about the requested queue.
        /// </summary>
        /// <param name="queueName">The Name of the queue to lookup.</param>
        /// <returns>QueueInfo object.  Null if not found.</returns>
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
        /// Disposes of storage resources
        /// </summary>
        public void Dispose()
        {
            // Need to add OnDsiapose action in case some storage services require disposal
        }

        /// <summary>
        /// Creates underlying structure in the storage layer.
        /// </summary>
        /// <param name="deleteExistingData">Will delete and recreate underlying structures.</param>
        /// <returns>Returns ValueTask.</returns>
        public async ValueTask InitializeStorage(bool deleteExistingData)
        {
            await this.storage.InitializeStorage(deleteExistingData);
        }

        /// <summary>
        /// Starts a transaction used my add message and pull message.
        /// </summary>
        /// <param name="expiryTimeInMin">Override default expiration time.</param>
        /// <returns>Transaction ID.</returns>
        public async ValueTask<long> StartTrasaction(int expiryTimeInMin = 0)
        {
            return await this.storage.StartTransaction(expiryTimeInMin);
        }

        /// <summary>
        /// Commits Transaction, updating all message in transaction.
        /// </summary>
        /// <param name="transactionId">Transaction Id to commit.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask CommitTrasaction(long transactionId)
        {
            // Validate Trans 1) exists, 2) Is active, 3) Is  not expired
            // Start DB Trans ---
            // Change status of added messages
            // Change status of pulled messages
            // Mark Transaction complete
            // Commit DB Trans ---
            throw new NotImplementedException();

            //return await this.storage.CommitTransaction(transactionId);
        }

        /// <summary>
        /// Rollsback a transaction, undoing any changes to messages in the transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID of the transaction to rollback.</param>
        /// <returns>ValueTask.</returns>
        public async ValueTask RollbackTrasaction(long transactionId)
        {
            throw new NotImplementedException();
            //return await this.storage.RollbackTransaction(transactionId);
        }
    }
}
