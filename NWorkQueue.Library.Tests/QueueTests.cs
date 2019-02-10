using System;
using System.Net.Sockets;
using NUnit;
using NUnit.Framework;
using NWorkQueue.Library;

namespace NWorkQueue.Library.Tests
{
    [TestFixture]
    public class QueueTests
    {
        [Test]
        public void InvalidCreateQueueName()
        {
            using (var api = new InternalApi(true))
            {
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.Queue.CreateQueue("(Peanuckle)"); }
                );
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.Queue.CreateQueue(""); }
                );
            }
        }

        [Test]
        public void DuplicateQueueName()
        {
            using (var api = new InternalApi(true))
            {
                api.Queue.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.Queue.CreateQueue("WiseMan"); }
                );
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.Queue.CreateQueue("wiseman"); }
                );
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"),
                    delegate { api.Queue.CreateQueue("WISEMAN"); }
                );
            }
        }

        [Test]
        public void InvalidDeleteQueueName()
        {
            using (var api = new InternalApi(true))
            {
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.Queue.CreateQueue("(Peanuckle)"); }
                );
                Assert.Throws(Is.TypeOf<ArgumentException>(), delegate { api.Queue.CreateQueue(""); }
                );
            }
        }

        [Test]
        public void DeleteQueueByName()
        {
            using (var api = new InternalApi(true))
            {
                api.Queue.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.Queue.DeleteQueue("WISEMEN"); }
                );
                api.Queue.DeleteQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.Queue.DeleteQueue("WiseMan"); }
                );
            }
        }

        [Test]
        public void DeleteQueueById()
        {
            using (var api = new InternalApi(true))
            {
                var queueId = api.Queue.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.Queue.DeleteQueue("WISEMEN"); }
                );
                api.Queue.DeleteQueue(queueId);
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.Queue.DeleteQueue(queueId); }
                );
            }
        }

        [Test]
        public void CheckDeletedQueueForMessages()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.Queue.CreateQueue("WiseMan");
                var trans = api.Transaction.StartTransaction();
                api.Message.AddMessage(trans, queue, new Object(), null);
                api.Transaction.CommitTransaction(trans);
                Assert.AreEqual(1, api.Transaction.GetMessageCount(queue));
                api.Queue.DeleteQueue(queue);
                Assert.AreEqual(0, api.Transaction.GetMessageCount(queue));
            }
        }
    }
}
 