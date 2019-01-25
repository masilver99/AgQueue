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
        public void DeleteQueueByName()
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
        public void DeleteQueueById()
        {
            using (var api = new InternalApi(true))
            {
                var queueId = api.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue("WISEMEN"); }
                );
                api.DeleteQueue(queueId);
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue(queueId); }
                );
            }
        }

        [Test]
        public void CheckDeletedQueueForMessages()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("WiseMan");
                var trans = api.StartTransaction();
                api.AddMessage(trans, queue, new Object(), null);
                api.CommitTransaction(trans);
                Assert.AreEqual(1, api.GetMessageCount(queue));
                api.DeleteQueue(queue);
                Assert.AreEqual(0, api.GetMessageCount(queue));
            }
        }
    }
}
 