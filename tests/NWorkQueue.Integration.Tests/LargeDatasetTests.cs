using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NWorkQueue.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NWorkQueue.Models;
using System.Threading.Tasks;
using Grpc.Net.ClientFactory;
using System;
using Grpc.Core;
using System.IO;
using NWorkQueue.Server.Common;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class LargeDatasetTests
    {
        private IWebHost? _webHost;

        public LargeDatasetTests()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        private IWebHost StartServer()
        {
            var webHostBuilder = CreateHostBuilder();
            var webHost = webHostBuilder.Build();
            webHost.Start();
            return webHost;
        }

        public static IWebHostBuilder CreateHostBuilder() =>
            WebHost.CreateDefaultBuilder(new string[0])
                .ConfigureServices(services =>
                { 
                    services.AddSingleton(new QueueOptions());
                })
                .ConfigureKestrel(option =>
                {
                    option.ListenLocalhost(10043, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                })
                .ConfigureServices(services => services.AddSingleton(new QueueOptions()))
                .UseStartup<StartupGrpc>();

        [TestInitialize]
        public void TestInitialize()
        {
            _webHost = StartServer();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            _webHost.ThrowIfNull();
            await _webHost.StopAsync();
            _webHost.WaitForShutdown();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            /*
            if (File.Exists("SqliteTesting.db"))
            {
                File.Delete("SqliteTesting.db");
            }
            */
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task Add1000MessagesOnetransaction()
        {
            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                MaxAttempts = 1
            };

            // Add Message
            for (int i = 0; i < 1000; i++)
            {
                var queueMessageResponse = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            }
            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task Add1000MessagesMultipleTransactions()
        {
            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var inMessage = new MessageIn()
            {
                MaxAttempts = 1
            };

            // Add Message
            for (int i = 0; i < 1000; i++)
            {
                var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());
                var queueMessageResponse = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
                await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task Pull1000MessagesInOrder()
        {
            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            // Add Messages
            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());
            for (int i = 0; i < 1000; i++)
            {
                var inMessage = new MessageIn()
                {
                    MaxAttempts = 1,
                    Payload = Google.Protobuf.ByteString.CopyFromUtf8(i.ToString())
                };

                var queueMessageResponse = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            }

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            var pullTrans = await client.StartTransactionAsync(new StartTransactionRequest());

            for (int i = 0; i < 1000; i++)
            {
                var message = await client.DequeueMessageAsync(new DequeueMessageRequest() { QueueId = createResponse.QueueId, TransId = pullTrans.TransId });
                Assert.AreEqual(message.Message.Payload.ToStringUtf8(), i.ToString());
            }

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = pullTrans.TransId });
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task Pull1000MessagesByPriority()
        {
            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            // Add Messages
            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());
            for (int i = 0; i < 1000; i++)
            {
                var priority = 1000 - 1;
                var inMessage = new MessageIn()
                {
                    MaxAttempts = 1,
                    Priority = priority,
                    Payload = Google.Protobuf.ByteString.CopyFromUtf8(priority.ToString())

                };

                var queueMessageResponse = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            }

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            var pullTrans = await client.StartTransactionAsync(new StartTransactionRequest());

            for (int i = 0; i < 1000; i++)
            {
                var message = await client.DequeueMessageAsync(new DequeueMessageRequest() { QueueId = createResponse.QueueId, TransId = pullTrans.TransId });
                Assert.AreEqual(message.Message.Payload.ToStringUtf8(), i.ToString());
            }

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = pullTrans.TransId });
        }


        private static async Task<QueueApi.QueueApiClient> CreateClient()
        {
            var channel = GrpcChannel.ForAddress("http://localhost:10043");
            var client = new QueueApi.QueueApiClient(channel);
            await client.InitializeStorageAsync(new InitializeStorageRequest { DeleteExistingData = true });
            return client;
        }
        
        private static async Task<QueueApi.QueueApiClient> CreateBadClient()
        {
            var channel = GrpcChannel.ForAddress("http://localhost:10043");
            var client = new QueueApi.QueueApiClient(channel);
            return client;
        }
    }
}
