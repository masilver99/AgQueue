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
using System.Reflection;
using System.Threading;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class TimeoutTests
    {
        private IWebHost? _webHost;

        public TimeoutTests()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        private IWebHost StartServer(QueueOptions? queueOptions)
        {
            var webHostBuilder = CreateHostBuilder(queueOptions ?? new QueueOptions());
            var webHost = webHostBuilder.Build();
            webHost.Start();
            return webHost;
        }

        public static IWebHostBuilder CreateHostBuilder(QueueOptions queueOptions) =>
            WebHost.CreateDefaultBuilder(new string[0])
                .ConfigureKestrel(option =>
                {
                    option.ListenLocalhost(10043, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                })
                .ConfigureServices(services => services.AddSingleton(queueOptions))
                .UseStartup<StartupGrpc>();

        public void TestInitialize(QueueOptions? queueOptions = null)
        {
            _webHost = StartServer(queueOptions);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            _webHost.ThrowIfNull();
            await _webHost.StopAsync();
            _webHost.WaitForShutdown();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists("SqliteTesting.db"))
            {
                File.Delete("SqliteTesting.db");
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task TimeoutInMessage()
        {
            this.TestInitialize(new QueueOptions
            {
                DefaultMessageTimeoutInMinutes = 10
            });

            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                ExpiryInMinutes = 1,
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            Thread.Sleep(1000 * 61);

            // Check Message is expired 
            var transPullResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var dequeueResponse = await client.DequeueMessageAsync(new DequeueMessageRequest { QueueId = 1, TransId = transPullResponse.TransId });

            Assert.IsNull(dequeueResponse.Message);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transPullResponse.TransId });

            var peekResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = 1 });

            Assert.AreEqual(MessageState.Expired, peekResponse.Message?.MessageState);
            Assert.AreNotEqual(0, peekResponse.Message?.CloseDateTime);
            Assert.AreEqual(0, peekResponse.Message?.TransId);
            Assert.AreEqual(TransactionAction.None, peekResponse.Message?.TransAction);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task MessageTimeoutInSettings()
        {
            this.TestInitialize(new QueueOptions
            {
                DefaultMessageTimeoutInMinutes = 1
            });

            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                ExpiryInMinutes = 0,   // Should default to the options timeout
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            Thread.Sleep(1000 * 30);

            var peekResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = queueMessageResponse1.MessageId });
            Assert.AreEqual(MessageState.Active, peekResponse.Message?.MessageState);

            Thread.Sleep(1000 * 31);

            // Check Message is expired 
            var transPullResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var dequeueResponse = await client.DequeueMessageAsync(new DequeueMessageRequest { QueueId = 1, TransId = transPullResponse.TransId });

            Assert.IsNull(dequeueResponse.Message);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transPullResponse.TransId });

            var peekResponse2 = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = 1 });

            Assert.AreEqual(MessageState.Expired, peekResponse2.Message?.MessageState);
            Assert.AreNotEqual(0, peekResponse2.Message?.CloseDateTime);
            Assert.AreEqual(0, peekResponse2.Message?.TransId);
            Assert.AreEqual(TransactionAction.None, peekResponse2.Message?.TransAction);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task TransactionTimeoutInSettingsAddMessage()
        {
            this.TestInitialize(new QueueOptions
            {
                DefaultTranactionTimeoutInMinutes = 1
            });

            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest() { ExpireInMin = 0 });

            var inMessage = new MessageIn()
            {
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            Thread.Sleep(1000 * 61);

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });
            });

            Assert.AreEqual("Transaction 1 not active: Expired", exception.Status.Detail);

            var peekResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = queueMessageResponse1.MessageId });
            Assert.IsNull(peekResponse.Message);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task TransactionTimeoutInTransactionAddMessage()
        {
            this.TestInitialize(new QueueOptions
            {
                DefaultTranactionTimeoutInMinutes = 10
            });

            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest() { ExpireInMin = 1 });

            var inMessage = new MessageIn()
            {
                MaxAttempts = 3
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            Thread.Sleep(1000 * 61);

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });
            });

            Assert.AreEqual("Transaction 1 not active: Expired", exception.Status.Detail);

            var peekResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = queueMessageResponse1.MessageId });
            Assert.IsNull(peekResponse.Message);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task TransactionTimeoutInTransactionPullMessage()
        {
            this.TestInitialize(new QueueOptions
            {
                DefaultTranactionTimeoutInMinutes = 10
            });

            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest() { ExpireInMin = 10 });

            var inMessage = new MessageIn()
            {
                MaxAttempts = 3
            };


            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            var commitTransResponse = await client.CommitTransactionAsync(new CommitTransactionRequest() { TransId = transResponse.TransId });

            var trans2Response = await client.StartTransactionAsync(new StartTransactionRequest() { ExpireInMin = 1 });

            var pullResponse = await client.DequeueMessageAsync(new DequeueMessageRequest { QueueId = 1, TransId = trans2Response.TransId });
            Assert.IsNotNull(pullResponse.Message);

            Thread.Sleep(1000 * 61);

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });
            });

            var peekResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = queueMessageResponse1.MessageId });
            Assert.AreEqual(MessageState.Expired, peekResponse.Message.MessageState);
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
