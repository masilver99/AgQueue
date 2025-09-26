// <copyright file="MessageState.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace Procession.Common
{
    /// <summary>
    /// Current state of a message.
    /// </summary>
    public enum MessageState
    {
        Unknown = 0,

        /// <summary>
        /// Gets active value.  Is the message is live and can be pulled from the queue.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Gets InTransaction.  Means the message is currently tied to a transaction, either during insert of during processing.  IOW, this message is currently being inserted or pulled from the queue.
        /// </summary>
        InTransaction = 2,

        /// <summary>
        /// Gets processed state. i.e. This message has been processed and will not be pulled.
        /// </summary>
        Processed = 3,

        /// <summary>
        /// Gets if Message has expired and will not be pulled.
        /// </summary>
        Expired = 4,

        /// <summary>
        /// Gets boolean representing if RetryExceeded. Message retry limit has been reached and message will no longer be pulled.
        /// </summary>
        AttemptsExceeded = 5
    }
}
