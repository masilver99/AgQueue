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
using NWorkQueue.Sqlite;

[assembly: InternalsVisibleTo("NWorkQueue.Tests")]

namespace NWorkQueue.Library
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
        /// <returns>A Queue object.</returns>
        public async ValueTask<(long QueueId, ApiResult ApiResult)> CreateQueue(string queueName)
        {
            var fixedName = this.StandardizeQueueName(queueName);

            // Check if Queue already exists
            if ((await this.storage.GetQueueId(fixedName)) != null)
            {
                return (0, new ApiResult { ResultCode = ResultCode.AlreadyExists, Message = $"Queue name {fixedName} already exists" });
            }

            return (await this.storage.AddQueue(queueName), new ApiResult { ResultCode = ResultCode.Ok });
        }

        private string StandardizeQueueName(string rawQueueName)
        {
            return rawQueueName.Replace(" ", string.Empty);
        }

        /// <summary>
        /// Delete a queue and all messages in the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue to delete.</param>
        public async ValueTask DeleteQueue(string queueName)
        {
            var fixedName = this.StandardizeQueueName(queueName);
            if (fixedName.Length == 0)
            {
                throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
            }

            var id = await this.storage.GetQueueId(fixedName);

            if (!id.HasValue)
            {
                throw new Exception("Queue not found");
            }

            this.DeleteQueue(id.Value);
        }

        /// <summary>
        /// Deletes a queue and 1) rollsback any transaction related to the queue, 2) deletes all messages in the queue.
        /// </summary>
        /// <param name="queueId">Queue id.</param>
        public void DeleteQueue(long queueId)
        {
            /*
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
            */
        }

        /// <summary>
        /// Returns a Queue object by name.
        /// </summary>
        /// <param name="queueName">The name of the queue to return.</param>
        /// <param name="autoCreate">If the name doesn't exist, create it otherwise throw an exception.</param>
        /// <returns>A Queue object.</returns>
        public Queue GetQueueByName(string queueName, bool autoCreate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a Queue object based on a queue ID.
        /// </summary>
        /// <param name="queueId">The Queue ID to lookup.</param>
        /// <returns>A Queue object.</returns>
        public Queue GetQueueById(long queueId)
        {
            throw new NotImplementedException();
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
