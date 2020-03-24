using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NWorkQueue.Common;
using NWorkQueue.Models;
using System.Threading.Tasks;
using Grpc.Net.ClientFactory;
using System;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class UnitTestGrpc
    {
        private IWebHost? _webHost;

        public UnitTestGrpc()
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
            await _webHost.StopAsync();
        }

        [TestMethod]
        //[DoNotParallelize]
        public async Task CreateQueue()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "Test" });

            Assert.AreEqual(1, createResponse.QueueId);
        }
        
        [TestMethod]
        //[DoNotParallelize]
        public async Task DeleteQueueById()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DeleteById" });
            Assert.AreEqual(1, createResponse.QueueId);
            var request = new DeleteQueueByNameRequest { QueueName = "DeleteById" };
            var queueInfoBefore = await client.QueueInfoByIdAsync(new QueueInfoByIdRequest { QueueId = 1 });
            Assert.AreEqual("DeleteById", queueInfoBefore.QueueName);
            Assert.AreEqual(1, queueInfoBefore.QueueId);
            await client.DeleteQueueByNameAsync(request);

            var queueInfoAfter = await client.QueueInfoByIdAsync(new QueueInfoByIdRequest { QueueId = 1 });
            Assert.IsFalse(queueInfoAfter.RecordFound);
        }

        [TestMethod]
        public async Task DeleteQueueByName()
        {
            var client = await CreateClient();

            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = "DeleteByName" });
            Assert.AreEqual(1, createResponse.QueueId);
            var request = new DeleteQueueByNameRequest { QueueName = "DeleteByName" };


            await client.DeleteQueueByNameAsync(request);
        }

        private static async Task<QueueApi.QueueApiClient> CreateClient()
        {
            var channel = GrpcChannel.ForAddress("http://localhost:10043");
            var client = new QueueApi.QueueApiClient(channel);
            await client.InitializeStorageAsync(new InitializeStorageRequest { DeleteExistingData = true });
            return client;
        }
    }
}
