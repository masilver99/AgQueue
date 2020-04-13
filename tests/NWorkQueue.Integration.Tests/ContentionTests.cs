using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NWorkQueue.Common.Extensions;
using NWorkQueue.Models;
using System.Threading.Tasks;
using Grpc.Net.ClientFactory;
using System;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class ContentionTests
    {
        private static IWebHost? _webHost;

        public ContentionTests()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        private static IWebHost StartServer()
        {
            var webHostBuilder = CreateHostBuilder();
            var webHost = webHostBuilder.Build();
            webHost.Start();
            return webHost;
        }

        public static IWebHostBuilder CreateHostBuilder() =>
            WebHost.CreateDefaultBuilder(new string[0])
            .UseShutdownTimeout(TimeSpan.FromSeconds(60))
                .ConfigureKestrel(option =>
                {
                    option.ListenLocalhost(10044, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }).UseStartup<StartupGrpc>();

        [ClassInitialize]
        public static void TestInitialize(TestContext testContext)
        {
            _webHost = StartServer();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _webHost.StopAsync(TimeSpan.FromSeconds(60));
            _webHost.WaitForShutdown();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task DequeueSingleMessagesOnThreads()
        {
            var dict = new ConcurrentDictionary<int, long?>(20, 20);

            var client = await CreateNewClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });
            Assert.AreEqual(1, createResponse.QueueId);

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            var queueMessageResponse2 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(2, queueMessageResponse2.MessageId);

            var queueMessageResponse3 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(3, queueMessageResponse3.MessageId);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            // We can't use async here as the Parallel library doesn't support await 
            Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (index) =>
            {
                var loopClient = CreateClient();

                var transPullResponse = loopClient.StartTransaction(new StartTransactionRequest());

                var dequeueResponse = loopClient.DequeueMessage(new DequeueMessageRequest { QueueId = 1, TransId = transPullResponse.TransId });

                dict.TryAdd(index, dequeueResponse.Message?.Id);

                loopClient.CommitTransaction(new CommitTransactionRequest { TransId = transPullResponse.TransId });
            });

            // Make sure three and only three messages were dequeued
            Assert.AreEqual(3, dict.Count(x => x.Value != null));
            Assert.AreEqual(1, dict.Count(x => x.Value == 1));
            Assert.AreEqual(1, dict.Count(x => x.Value == 2));
            Assert.AreEqual(1, dict.Count(x => x.Value == 3));
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task RejectSingleMessageOnThreads()
        {
            var dict = new ConcurrentDictionary<int, long?>(20, 20);

            var client = await CreateNewClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });
            Assert.AreEqual(1, createResponse.QueueId);

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            var queueMessageResponse2 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(2, queueMessageResponse2.MessageId);

            var queueMessageResponse3 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(3, queueMessageResponse3.MessageId);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (index) =>
            {
                var loopClient = CreateClient();

                var transPullResponse = loopClient.StartTransaction(new StartTransactionRequest());

                var dequeueResponse = loopClient.DequeueMessage(new DequeueMessageRequest { QueueId = 1, TransId = transPullResponse.TransId });

                dict.TryAdd(index, dequeueResponse.Message?.Id);

                // Pretend there is something wrong with message 2.  Keep rolling it back.
                if (dequeueResponse.Message?.Id == 2)
                {
                    loopClient.RollbackTranaction(new RollbackTransactionRequest { TransId = transPullResponse.TransId });
                }
                else
                {
                    loopClient.CommitTransaction(new CommitTransactionRequest { TransId = transPullResponse.TransId });
                }

            });

            // These are highly dependent on the threading.  They could fail if the threads finish too quickly.  Adding more threads should solve it.
            Assert.AreEqual(5, dict.Count(x => x.Value != null));
            Assert.AreEqual(1, dict.Count(x => x.Value == 1));
            Assert.AreEqual(3, dict.Count(x => x.Value == 2));
            Assert.AreEqual(1, dict.Count(x => x.Value == 3));

            var messageCheckResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = 2 });
            Assert.AreEqual(3, messageCheckResponse.Message?.Attempts);
            Assert.AreEqual(2, messageCheckResponse.Message?.Id);  //Just in case
            Assert.AreEqual(3, messageCheckResponse.Message?.MaxAttempts);
            Assert.AreEqual(MessageState.AttemptsExceeded, messageCheckResponse.Message?.MessageState);
            Assert.AreNotEqual(0, messageCheckResponse.Message?.CloseDateTime);
        }


        private static async Task<QueueApi.QueueApiClient> CreateNewClient()
        {
            var channel = GrpcChannel.ForAddress("http://localhost:10044");
            var client = new QueueApi.QueueApiClient(channel);
            await client.InitializeStorageAsync(new InitializeStorageRequest { DeleteExistingData = true });
            return client;
        }

        private static QueueApi.QueueApiClient CreateClient()
        {
            var channel = GrpcChannel.ForAddress("http://localhost:10044");
            var client = new QueueApi.QueueApiClient(channel);
            return client;
        }

        private static async Task<QueueApi.QueueApiClient> CreateBadClient()
        {
            File.Delete("SqliteTesting.db");  // hard reset of the database.
            var channel = GrpcChannel.ForAddress("http://localhost:10044");
            var client = new QueueApi.QueueApiClient(channel);
            return client;
        }
    }
}
