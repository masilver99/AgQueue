using System;
using System.Net.Sockets;
using NUnit;
using NUnit.Framework;
using NWorkQueue.Library;

namespace NWorkQueue.Library.Tests
{
    [TestFixture]
    public class MessageTests
    {
        [Test]
        public void Add10000Messages()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("WiseMan");
                var trans = api.StartTransaction();
                for (int i = 0; i < 10000; i++)
                {
                    api.AddMessage(trans, queue, new object(), "");
                }
                var messageCount = api.GetMessageCount(queue);
                Assert.AreEqual(0, messageCount);
                api.CommitTransaction(trans);
                messageCount = api.GetMessageCount(queue);
                Assert.AreEqual(10000, messageCount);
            }
        }
        /* Not sure this will be possible in updated api
        [Test]
        public void Add10000MessagesInTrans()
        {
            object[] array = new object[10000];
            for (int i = 0; i < 10000; i++)
            {
                array[i] = new object();
            }

            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("WiseMan");
                var trans = api.StartTransaction();
                queue.AddMessage(trans, array, 0);
                trans.Commit();
            }
        }
        */
    }
}
 