using System;
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

    }
}
