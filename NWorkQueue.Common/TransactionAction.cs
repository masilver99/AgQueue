// <copyright file="TransactionAction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    /// <summary>
    /// Represents the action performed within a transaction.
    /// </summary>
    public sealed class TransactionAction
    {
        /// <summary>
        /// Item was added within a transaction (if rolledback, delete).
        /// </summary>
        public static readonly TransactionAction Add = new TransactionAction("Add", 0);

        /// <summary>
        /// Item was pulled within transaction, increment retry count if rolledback.
        /// </summary>
        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        private TransactionAction(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets transaction action name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets transaction Action value / id.
        /// </summary>
        public int Value { get; }
    }
}
