// <copyright file="Message.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Server.Common
{
    using System;
    using System.Threading;
    using MessagePack;
    using NWorkQueue.Common;

    /// <summary>
    /// APIS's for accessing and manging queue messages.
    /// </summary>
    public class Message
    {
        private readonly IStorage storage;

        private long currMessageId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="storage">Storage library.</param>
        internal Message(IStorage storage)
        {
            /*
            this.storage = storage;
            this.currMessageId = this.storage.GetMaxMessageId();
            */
        }
    }
}
