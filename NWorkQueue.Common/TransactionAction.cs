// <copyright file="TransactionAction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    public sealed class TransactionAction
    {
        public static readonly TransactionAction Add = new TransactionAction("Add", 0);
        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        private TransactionAction(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; }

        public int Value { get; }
    }
}
