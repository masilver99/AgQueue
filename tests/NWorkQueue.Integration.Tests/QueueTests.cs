using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NWorkQueue.Integration.Tests
{

    public class QueueTests
    {/*
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
                var queue = api.CreateQueue("WiseMan");
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue("WISEMEN"); }
                );
                api.DeleteQueue(queue.Id);
                Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"),
                    delegate { api.DeleteQueue(queue.Id); }
                );
            }
        }

        [Test]
        public void CheckDeletedQueueForMessages()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("WiseMan");
                var trans = api.CreateTransaction();
                queue.AddMessage(trans, new Object(), null);
                trans.Commit();
                Assert.AreEqual(1, queue.GetMessageCount());
                api.DeleteQueue(queue.Id);
                Assert.AreEqual(0, queue.GetMessageCount());
            }
        }*/
    }
}
 