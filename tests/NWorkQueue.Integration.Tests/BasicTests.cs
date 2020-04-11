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
using Grpc.Core;
using System.IO;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class BasicTests
    {
        private IWebHost? _webHost;

        public BasicTests()
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
                .ConfigureKestrel(option =>
                {
                    option.ListenLocalhost(10043, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }).UseStartup<StartupGrpc>();

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
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task CreateQueue()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "Test" });
            Assert.AreEqual(1, createResponse.QueueId);

            var queueInfo = await client.GetQueueInfoByIdAsync(new GetQueueInfoByIdRequest { QueueId = 1 });
            Assert.AreEqual("Test", queueInfo.QueueName);
            Assert.IsTrue(queueInfo.RecordFound);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task DeleteQueueById()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DeleteById" });
            Assert.AreEqual(1, createResponse.QueueId);
            var request = new DeleteQueueByNameRequest { QueueName = "DeleteById" };
            var queueInfoBefore = await client.GetQueueInfoByIdAsync(new GetQueueInfoByIdRequest { QueueId = 1 });
            Assert.AreEqual("DeleteById", queueInfoBefore.QueueName);
            Assert.AreEqual(1, queueInfoBefore.QueueId);
            await client.DeleteQueueByNameAsync(request);

            var queueInfoAfter = await client.GetQueueInfoByIdAsync(new GetQueueInfoByIdRequest { QueueId = 1 });
            Assert.IsFalse(queueInfoAfter.RecordFound);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task DeleteQueueByName()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DeleteByName" });
            Assert.AreEqual(1, createResponse.QueueId);
            var request = new DeleteQueueByNameRequest { QueueName = "DeleteByName" };

            await client.DeleteQueueByNameAsync(request);

            var queueInfoAfter = await client.GetQueueInfoByNameAsync(new GetQueueInfoByNameRequest { QueueName = "DeleteByName" });
            Assert.IsFalse(queueInfoAfter.RecordFound);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task TestExceptionInterceptor()
        {
            var client = await CreateBadClient();

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "Test" });
            });

            Assert.AreEqual(StatusCode.Internal, exception.Status.StatusCode);
            Assert.AreEqual("SQLite Error 1: 'no such table: Queues'.", exception.Status.Detail);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task DequeueMessageDefault()
        {
            var client = await CreateClient();

            // Test Create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });
            Assert.AreEqual(1, createResponse.QueueId);
            var extraQueueResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "ExtraQueue" });
            Assert.AreEqual(2, extraQueueResponse.QueueId);

            var transResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var inMessage = new MessageIn()
            {
                
            };

            // Add Message
            var queueMessageResponse1 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(1, queueMessageResponse1.MessageId);

            var queueMessageResponse2 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(2, queueMessageResponse2.MessageId);

            var queueMessageResponse3 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            Assert.AreEqual(3, queueMessageResponse3.MessageId);

            // Check Message is NOT yet in the queue (since transaction not committed)
            var messageCheckResponse = await client.PeekMessageByIdAsync(new PeekMessageByIdRequest { MessageId = 2 });
            Assert.AreEqual(2, messageCheckResponse.Message.Id);

            await client.CommitTransactionAsync(new CommitTransactionRequest { TransId = transResponse.TransId });

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                var queueMessageResponse4 = await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, QueueId = 1, TransId = transResponse.TransId });
            });

            Assert.AreEqual(StatusCode.Internal, exception.Status.StatusCode);
            Assert.AreEqual("Transaction 1 not active: Commited", exception.Status.Detail);


            await _webHost.StopAsync();
            
            _webHost = StartServer();

            var transPullResponse = await client.StartTransactionAsync(new StartTransactionRequest());

            var dequeueResponse = await client.DequeueMessageAsync(new DequeueMessageRequest { QueueId = 1, TransId = transPullResponse.TransId });

            Assert.AreEqual(1, dequeueResponse.Message.Id);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task QueueMessageWithoutTrans()
        {
            var client = await CreateClient();

            // create quque
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DefaultDeququeTest" });
            Assert.AreEqual(1, createResponse.QueueId);
            var extraQueueResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "ExtraQueue" });
            Assert.AreEqual(2, extraQueueResponse.QueueId);

            var inMessage = new MessageIn()
            {

            };

            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            {
                await client.QueueMessageAsync(new QueueMessageRequest { Message = inMessage, TransId = 0 });
            });

            Assert.AreEqual("Transaction not found, id: 0", exception.Status.Detail);
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
            if (File.Exists("SqliteTesting.db"))
            {
                File.Delete("SqliteTesting.db");
            }
            var channel = GrpcChannel.ForAddress("http://localhost:10043");
            var client = new QueueApi.QueueApiClient(channel);
            return client;
        }
    }
}
