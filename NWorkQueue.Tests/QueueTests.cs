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
            var api = new Api(true);
            Assert.Throws(Is.TypeOf<ArgumentException>(), delegate {
                api.CreateQueue("(Peanuckle)");
            }
            );
            Assert.Throws(Is.TypeOf<ArgumentException>(), delegate {
                    api.CreateQueue("");
                }
            );

        }

        [Test]
        public void DuplicateQueueName()
        {
            var api = new Api(true);
            api.CreateQueue("WiseMan");
            Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"), delegate {
                    api.CreateQueue("WiseMan");
                }
            );
            Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"), delegate {
                    api.CreateQueue("wiseman");
                }
            );
            Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue already exists"), delegate {
                    api.CreateQueue("WISEMAN");
                }
            );
        }

        [Test]
        public void InvalidDeleteQueueName()
        {
            var api = new Api(true);
            Assert.Throws(Is.TypeOf<ArgumentException>(), delegate {
                    api.CreateQueue("(Peanuckle)");
                }
            );
            Assert.Throws(Is.TypeOf<ArgumentException>(), delegate {
                    api.CreateQueue("");
                }
            );
        }

        [Test]
        public void DeleteQueue()
        {
            var api = new Api(true);
            api.CreateQueue("WiseMan");
            Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"), delegate {
                    api.DeleteQueue("WISEMEN");
                }
            );
            api.DeleteQueue("WiseMan");
            Assert.Throws(Is.TypeOf<Exception>().And.Message.EqualTo("Queue not found"), delegate {
                    api.DeleteQueue("WiseMan");
                }
            );

        }
        [Test]
        public void Add10000Messages()
        {
            var api = new Api(true);
            var queue = api.CreateQueue("WiseMan");
            var trans = api.StartTransaction();
            for (int i = 0; i < 10000; i++)
            {
                queue.AddMessage(trans, new object(), 0);
            }
            trans.Commit();
        }

        [Test]
        public void Add10000MessagesInTrans()
        {
            object[] array = new object[10000];
            for (int i = 0; i < 10000; i++)
            {
                array[i] = new object();
            }

            var api = new Api(true);
            var queue = api.CreateQueue("WiseMan");
            var trans = api.StartTransaction();
            queue.AddMessage(trans, array, 0);
            trans.Commit();
        }
    }
}
 