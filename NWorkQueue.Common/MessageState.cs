// <copyright file="MessageState.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    /// <summary>
    /// Current state of a message
    /// </summary>
    public sealed class MessageState
    {
        private MessageState(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets active value.  Is the message is live and can be pulled from the queue
        /// </summary>
        public static MessageState Active => new MessageState("Active", 0);

        /// <summary>
        /// Gets InTransaction.  Means the message is currently tied to a transaction, either during insert of during processing.  IOW, this message is currently being inserted or pulled from the queue.
        /// </summary>
        public static MessageState InTransaction => new MessageState("InTransaction", 1);

        /// <summary>
        /// Gets processed state. i.e. This message has been processed and will not be pulled
        /// </summary>
        public static MessageState Processed => new MessageState("Processed", 2);

        /// <summary>
        /// Gets if Message has expired and will not be pulled
        /// </summary>
        public static MessageState Expired => new MessageState("Expired", 3);

        /// <summary>
        /// Gets boolean representing if RetryExceeded. Message retry limit has been reached and message will no longer be pulled
        /// </summary>
        public static MessageState RetryExceeded => new MessageState("RetryExceeded", 4);

        /// <summary>
        /// Gets the message state string
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the message state ID
        /// </summary>
        public int Value { get; }
    }
}
