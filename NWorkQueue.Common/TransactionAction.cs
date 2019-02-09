namespace NWorkQueue.Common
{
    public sealed class TransactionAction
    {
        private TransactionAction(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }

        public static readonly TransactionAction Add = new TransactionAction("Add", 0);
        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        public string Name { get; }

        public int Value { get; }
    }
}
