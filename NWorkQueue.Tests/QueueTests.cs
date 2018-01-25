using System;
using System.Net.Sockets;
using NUnit;
using NUnit.Framework;
using NWorkQueue.Library;

namespace NWorkQueue.Tests
{
    [TestFixture]
    public class QueueTests
    {
        [Test]
        public void InvalidCreateQueueName()
        {
            using (var api = new InternalApi(true))
            {
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.CreateQueue("(Peanuckle)"); }
                );
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.CreateQueue(""); }
                );
            }
        }

        [Test]
        public void DuplicateQueueName()
        {
            using (var api = new InternalApi(true))
            {
                api.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.CreateQueue("WiseMan"); }
                );
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.CreateQueue("wiseman"); }
                );
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.CreateQueue("WISEMAN"); }
                );
            }
        }

        [Test]
        public void InvalidDeleteQueueName()
        {
            using (var api = new InternalApi(true))
            {
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.CreateQueue("(Peanuckle)"); }
                );
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.CreateQueue(""); }
                );
            }
        }

        [Test]
        public void DeleteQueue()
        {
            using (var api = new InternalApi(true))
            {
                api.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue("WISEMEN"); }
                );
                api.DeleteQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue("WiseMan"); }
                );
            }
        }
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

                api.CommitTransaction(trans);
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
 