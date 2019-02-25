namespace NWorkQueue.Library.Tests.Scenarios
{
    using NUnit;
    using NUnit.Framework;
    using NWorkQueue.Library;

    /// <summary>
    /// Scenarios define the precisie expections of NWorkQueue with certain workflows.  The build upon simple unit tests in many cases.  They go from simple to complex.
    /// </summary>
    [TestFixture]
    public class ScenarioTests
    {
        /* Scenario #3: Pull Message
           Operations:
             1) Start Transaction
             2) Add Message
             3) Commit Transaction
             4) Pull Message
           Expectations:
             A) No Message in queue
        */

        /* Scenario #4: Pull Message, Rollback pull
        */
        /* Scenario #5: Add message, let expire
        */
        /* Scenario #6: Add message, pass retry threshold
        */
        /* Scenario #7: Peek specific message
        */
        /* Scenario #8: Pull Message by Correlation 
        */
        /* Scenario #9: Pull Message by Group name
        */
        /* Scenario #10: Pull Message by priority
        */
        /* Scenario #11: Delete queue
        */
        /* Scenario #12: Expired Transaction
        */
        /* Scenario #13: 
        */
        /* Scenario #14: 
        */
        /* Scenario #15: 
        */
        /* Scenario #16: 
        */

        /// Scenario #1: Add message
        /// Operations:  
        ///   1) Start Transaction
        ///   2) Add message
        ///   3) Commit Transaction
        /// Expectations: 
        ///   A) Message is in Queue
        ///   B) Transaction is closed
        [Test]
        public void AddMessage()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.Queue.Create("Scenario1");
                var trans = api.Transaction.Start();
                var messageId = api.Message.Add(trans, queue, "blah", "");
                var messageCount = api.Queue.GetMessageCount(queue);
                Assert.AreEqual(0, messageCount);
                api.Transaction.Commit(trans);
                messageCount = api.Queue.GetMessageCount(queue);
                Assert.AreEqual(1, messageCount);
            }
        }

        /* Scenario #2: Rollback add message
           Operations:
             1) Start Transaction
             2) Add Message
             3) Rollback Transaction
           Expectations:
             A) No Message in queue
        */
        [Test]
        public void AddMessagePullMessage()
        {
            using (var api = new InternalApi(true))
            {
                var queueId = api.Queue.Create("Scenario1");
                var transId = api.Transaction.Start();
                var messageId = api.Message.Add(transId, queueId, "blah", "");
                api.Transaction.Commit(transId);
                transId = api.Transaction.Start();
                api.Message.PullMessage(queueId, transId);
                var messageCount = api.Queue.GetMessageCount(queueId);
                Assert.AreEqual(1, messageCount);
            }
        }
    }
}