using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Grpc.Client;
using Grpc.Core;
using ProtoBuf.Grpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NWorkQueue.Common;
using NWorkQueue.Models;
using System.Threading.Tasks;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class UnitTestGrpc
    {
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

    
        [TestMethod]
        public async Task CreateQueue()
        {
            var webHost = StartServer();
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var channel = GrpcChannel.ForAddress("http://localhost:10043");
            var client = new QueueApi.QueueApiClient(channel);
            //await service.InitializeStorage(new InitializeStorageRequest { DeleteExistingData = true });
            var createResponse = client.CreateQueue(new CreateQueueRequest() /*{ QueueName = "Test" }*/);
            //Assert.AreEqual(1, createResponse.QueueId);
            //var result = createResponse.Result;
            await webHost.StopAsync();
        }
        /*
        [TestMethod]
        public async Task DeleteQueueById()
        {
            var webHost = StartServer();
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var service = http.CreateGrpcService<IQueueApi>();
            await service.InitializeStorage(new InitializeStorageRequest { DeleteExistingData = true });
            var createResponse = await service.CreateQueue(new Common.Models.CreateQueueRequest { QueueName = "DeleteById" });
            Assert.AreEqual(1, createResponse.QueueId);
            var request = new Common.Models.DeleteQueueByNameRequest { QueueName = "blah" };
            await service.DeleteQueueByName(request);


            //var result = createResponse.Result;
            await webHost.StopAsync();
        }
        */
    }
}
