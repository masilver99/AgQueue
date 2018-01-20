using System;
using System.Threading;
using NWorkQueue.Library;
using NLog;

namespace NWorkQueue
{
    class Program
    {
        
        private static Boolean _performShutdown = false;
        static void Main(string[] args)
        {
            LogManager.LoadConfiguration("nlog.config");
            var log = LogManager.GetLogger("NWorkQueue");
            log.Trace("Application starting...");

            //Ensure we stop when asked to or CTRL-C is pressed
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { log.Trace("Application shutdown requested.  Initiating shutdown..."); _performShutdown = true; };
            Console.CancelKeyPress += (sender, eventArgs) => { log.Trace("Ctrl-C pressed.  Initiating shutdown..."); _performShutdown = true; };


            log.Trace("Starting communication threads...");

            log.Trace("Starting internal loop...");
            //Loop here until Ctrl-C or shutdown is requested
            while (!_performShutdown)
            {

                Thread.Sleep(500); 
                Console.WriteLine("Waiting....");
            }

            //Perform cleanup
            Console.Read();

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
