using NWorkQueue.Server.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Integration.Tests
{
    public static class QueueOptionsFactory
    {
        public static QueueOptions DefaultQueueOptions =>
            new QueueOptions 
            {
                DefaultMessageTimeoutInMinutes = 10,
                DefaultTranactionTimeoutInMinutes = 10
            };
    }
}
