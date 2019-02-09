namespace NWorkQueue.Common
{
    public sealed class MessageState
    {
        private MessageState(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Active means the message is live and can be pulled from the queue
        /// </summary>
        public static MessageState Active => new MessageState("Active", 0);

        /// <summary>
        /// InTransaction means the message is currently tied to a transaction, either during insert of during processing.  IOW, this message is currently being inserted or pulled from the queue.
        /// </summary>
        public static MessageState InTransaction => new MessageState("InTransaction", 1);

        /// <summary>
        /// This message has been processed and will not be pulled
        /// </summary>
        public static MessageState Processed => new MessageState("Processed", 2);

        /// <summary>
        /// Message has expired and will not be pulled
        /// </summary>
        public static MessageState Expired => new MessageState("Expired", 3);

        /// <summary>
        /// Message retry limit has been reached and message will no longer be pulled
        /// </summary>
        public static MessageState RetryExceeded => new MessageState("RetryExceeded", 4);

        public string Name { get; }

        public int Value { get; }
    }
}
