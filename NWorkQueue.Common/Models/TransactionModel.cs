using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue
{
    public class TransactionModel
    {
        public int Id { get; set; }

        public bool Active { get; set; }

        public DateTime CreateDateTime { get; set; }

        public DateTime ExpiryDateTime { get; set; }
    }
}
