using System;
using NWorkQueue.Library;

namespace NWorkQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var c = new Api();
            var tableName = "Testing-1-2-3";

            //Initialize storage
            //Perform cleanup
            // + clear expired transactions


        }

        //CreateQueue
        //DeleteQueue

        //AddMessage(w/ or w/o trans)
        //DeleteMessage
        //DeleteAllMessages
        //GetNextMessage

        //CreateTransaction
        //CommitTransaction
        //RollbackTransaction

        //uses cases
        //1 pull message, rollback pull
        //Mark message as inprocess and add transaction
        //trans is rolled back, message is marked available, trans is marked as rolledback

        //transaction -> undoaction

    }
}
