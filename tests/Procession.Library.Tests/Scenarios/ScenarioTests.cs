namespace Procession.Library.Tests.Scenarios
{
    using NUnit;
    using NUnit.Framework;
    using AgQueue.Library;

    /// <summary>
    /// Scenarios define the precisie expections of AgQueue with certain workflows.  The build upon simple unit tests in many cases.  They go from simple to complex.
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
        /*[Test]
        public void AddMessage()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("Scenario1");
                var trans = api.CreateTransaction();
                var messageId = queue.AddMessage(trans, "blah", "");
                var messageCount = queue.GetMessageCount();
                Assert.AreEqual(0, messageCount);
                trans.Commit();
                messageCount = queue.GetMessageCount();
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
        /*
        [Test]
        public void AddMessagePullMessage()
        {
            using (var api = new InternalApi(true))
            {
                var queue = api.CreateQueue("Scenario1");
                var trans = api.CreateTransaction();
                var messageId = queue.AddMessage(trans, "blah", "");
                trans.Commit();
                //queue.PullMessage(queueId, transId);
                var messageCount = queue.GetMessageCount();
                Assert.AreEqual(1, messageCount);
            }
        }*/
    }
}